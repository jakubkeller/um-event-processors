using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ServiceClients.Navitus.MedicationHistory;
using Xunit;

namespace UnitTests.ServiceClients;

public class MSAuthTokenProviderTests
{
    private const string TestAzp = "test-entra-client-id";

    private static MedicationHistoryClientOptions DefaultOptions => new()
    {
        BaseUrl = "https://rxhistory-test.api.navitus.com",
        EntraTokenUrl = "https://navitustest.ciamlogin.com/test-tenant/oauth2/v2.0/token",
        EntraClientId = "test-entra-client-id",
        EntraClientSecret = "test-entra-secret",
        EntraScope = "api://test-app/.default",
        MsAuthEnv = "Test"
    };

    private static string BuildFakeJwt(string azp)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\"}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { azp, sub = "object-id-not-used" })));
        return $"{header}.{payload}.fakesig";
    }

    private static string EntraTokenResponse(string azp) =>
        JsonSerializer.Serialize(new { access_token = BuildFakeJwt(azp) });

    [Fact]
    public async Task GetTokenAsync_EntraFlow_ReturnsMSAuthToken()
    {
        var handler = new FakeHttpHandler(EntraTokenResponse(TestAzp));
        var provider = new MSAuthTokenProvider(new HttpClient(handler), Options.Create(DefaultOptions), new MemoryCache(new MemoryCacheOptions()));

        var token = await provider.GetTokenAsync();

        token.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(token).Should().NotBeEmpty();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_CachesToken_SecondCallSkipsHttp()
    {
        var handler = new FakeHttpHandler(EntraTokenResponse(TestAzp));
        var provider = new MSAuthTokenProvider(new HttpClient(handler), Options.Create(DefaultOptions), new MemoryCache(new MemoryCacheOptions()));

        var token1 = await provider.GetTokenAsync();
        var token2 = await provider.GetTokenAsync();

        token1.Should().Be(token2);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_EntraFails_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized);
        var provider = new MSAuthTokenProvider(new HttpClient(handler), Options.Create(DefaultOptions), new MemoryCache(new MemoryCacheOptions()));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_MissingEntraTokenUrl_ThrowsInvalidOperationException()
    {
        var opts = DefaultOptions;
        opts.EntraTokenUrl = null;
        var provider = new MSAuthTokenProvider(new HttpClient(new FakeHttpHandler(HttpStatusCode.OK)), Options.Create(opts), new MemoryCache(new MemoryCacheOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_JwtMissingAzp_ThrowsInvalidOperationException()
    {
        var noAzpJwt = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\"}"))
            + "." + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"oid-only\"}"))
            + ".fakesig";
        var handler = new FakeHttpHandler(JsonSerializer.Serialize(new { access_token = noAzpJwt }));
        var provider = new MSAuthTokenProvider(new HttpClient(handler), Options.Create(DefaultOptions), new MemoryCache(new MemoryCacheOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
    }

    [Fact]
    public void BuildMSAuthToken_DeserializesToValidClaimsPrincipal()
    {
        var token = MSAuthTokenProvider.BuildMSAuthToken("test-client-id", "APIM", "Test");
        var bytes = Convert.FromBase64String(token);

        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);
        var principal = new ClaimsPrincipal(reader);

        principal.Identity!.AuthenticationType.Should().Be("MSAuth");
        principal.FindFirstValue(ClaimTypes.Name).Should().Be("APIM");
        principal.FindFirstValue("exp").Should().NotBeNullOrEmpty();
        principal.FindFirstValue("env").Should().Be("Test");
        principal.FindFirstValue("xid").Should().Be("test-client-id");
        principal.FindFirstValue("sig").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BuildMSAuthToken_SignatureMatchesExpectedComputation()
    {
        var token = MSAuthTokenProvider.BuildMSAuthToken("test-client-id", "APIM", "Test");
        var bytes = Convert.FromBase64String(token);

        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);
        var principal = new ClaimsPrincipal(reader);

        var exp = principal.FindFirstValue("exp")!;
        var sig = principal.FindFirstValue("sig")!;

        var expectedSig = MSAuthTokenProvider.ComputeSignature("APIM", exp, "Test", "test-client-id");
        sig.Should().Be(expectedSig);
        expectedSig.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    private class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string? _responseBody;
        private readonly HttpStatusCode _statusCode;
        public int CallCount { get; private set; }

        public FakeHttpHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        public FakeHttpHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new HttpResponseMessage(_statusCode);
            if (_responseBody != null)
                response.Content = new StringContent(_responseBody);
            return Task.FromResult(response);
        }
    }
}
