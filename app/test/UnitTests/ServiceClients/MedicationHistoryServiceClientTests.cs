using System.Net;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using ServiceClients.Navitus.MedicationHistory;
using Xunit;

namespace UnitTests.ServiceClients;

public class MedicationHistoryServiceClientTests
{
    private readonly IMSAuthTokenProvider _tokenProvider;

    public MedicationHistoryServiceClientTests()
    {
        _tokenProvider = Substitute.For<IMSAuthTokenProvider>();
        _tokenProvider.GetTokenAsync(Arg.Any<CancellationToken>()).Returns("fake-msauth-token");
    }

    private MedicationHistoryServiceClient CreateClient(HttpClient httpClient) =>
        new(httpClient, _tokenProvider);

    [Fact]
    public async Task MemberAsync_Success_ReturnsTransactions()
    {
        var expected = new[] { new Transaction { Ndc = "12345678901", PharmacyNumber = "1234567", DaySupply = 30 } };
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expected))
        });
        var client = CreateClient(new HttpClient(handler) { BaseAddress = new Uri("https://test.api/") });

        var result = await client.MemberAsync(
            DateTimeOffset.Now.AddYears(-1), DateTimeOffset.Now, ClaimStatuses.All,
            new MedicationHistoryRequest { SubscriberNumber = "SUB001", PersonCode = "01" });

        result.Result.Should().HaveCount(1);
        result.Result.First().Ndc.Should().Be("12345678901");
        result.Result.First().DaySupply.Should().Be(30);
    }

    [Fact]
    public async Task MemberAsync_SetsMSAuthHeader()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        });
        var client = CreateClient(new HttpClient(handler) { BaseAddress = new Uri("https://test.api/") });

        await client.MemberAsync(
            null, null, ClaimStatuses.All,
            new MedicationHistoryRequest { SubscriberNumber = "SUB001", PersonCode = "01" });

        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("MSAuth");
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("fake-msauth-token");
        await _tokenProvider.Received(1).GetTokenAsync(Arg.Any<CancellationToken>());
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }
}
