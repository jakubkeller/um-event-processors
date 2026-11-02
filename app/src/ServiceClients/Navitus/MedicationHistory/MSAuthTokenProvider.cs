using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ServiceClients.Navitus.MedicationHistory;

/// <summary>
/// Acquires MSAuth tokens for Gen 1 on-prem API authentication.
///
/// Flow: Entra client_credentials → bearer JWT → decode azp claim → build MSAuth token locally.
///
/// The token is built locally (rather than exchanged via APIM) because APIM reads the JWT "sub"
/// claim as the xid, but for Entra JWTs "sub" is the object ID — not the client_id.
/// Carrier permissions in the Microservice Security Wizard are mapped to the client_id,
/// which is in the "azp" claim. Building locally lets us use "azp" as the xid.
///
/// Token construction uses ClaimsPrincipal.WriteTo (same serialization as
/// Navitus.Microservice.Common.AuthHandler.GenerateToken). The RxHistory service
/// validates tokens with Common's AuthHandler, which expects this exact binary format.
///
/// Tokens are cached in memory. Cache and expiry durations are driven by MsAuthEnv:
///   Test  → exp 60 min, cache 55 min
///   Prod  → exp 5 min,  cache 4 min
/// </summary>
public class MSAuthTokenProvider : IMSAuthTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly MedicationHistoryClientOptions _options;
    private readonly IMemoryCache _cache;
    private readonly string _env;
    private readonly TimeSpan _tokenExpiry;
    private readonly TimeSpan _cacheDuration;
    private const string CacheKey = "MSAuthToken_MedicationHistory";
    private const string MsAuthName = "APIM";

    public MSAuthTokenProvider(HttpClient httpClient, IOptions<MedicationHistoryClientOptions> options, IMemoryCache cache)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _env = _options.MsAuthEnv;
        var isProd = string.Equals(_env, "Prod", StringComparison.OrdinalIgnoreCase);
        _tokenExpiry = isProd ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(60);
        _cacheDuration = isProd ? TimeSpan.FromMinutes(4) : TimeSpan.FromMinutes(55);
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<string>(CacheKey, out var cached) && !string.IsNullOrEmpty(cached))
            return cached;

        Logger.LogInformation("MSAuthTokenProvider: cache miss, acquiring new MSAuth token");

        var tokenEndpoint = _options.EntraTokenUrl
            ?? throw new InvalidOperationException("MedicationHistory:EntraTokenUrl is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.EntraClientId,
                ["client_secret"] = _options.EntraClientSecret,
                ["scope"] = _options.EntraScope
            })
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var accessToken = JsonSerializer.Deserialize<TokenResponse>(body)?.AccessToken
            ?? throw new InvalidOperationException("Entra returned null access_token.");

        var azp = DecodeJwtClaim(accessToken, "azp")
            ?? throw new InvalidOperationException("Entra JWT missing 'azp' claim.");

        var msAuthToken = BuildMSAuthToken(azp, MsAuthName, _env, _tokenExpiry);
        _cache.Set(CacheKey, msAuthToken, _cacheDuration);

        return msAuthToken;
    }

    /// <summary>
    /// Builds an MSAuth token using ClaimsPrincipal.WriteTo — the same binary serialization
    /// used by Navitus.Microservice.Common.AuthHandler.GenerateToken.
    /// </summary>
    public static string BuildMSAuthToken(string xid, string name = MsAuthName, string env = "Test", TimeSpan? expiry = null)
    {
        var exp = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(60)).ToUnixTimeSeconds().ToString();
        var sig = ComputeSignature(name, exp, env, xid);

        var identity = new ClaimsIdentity("MSAuth");
        identity.AddClaim(new Claim(ClaimTypes.Name, name));
        identity.AddClaim(new Claim("exp", exp));
        identity.AddClaim(new Claim("env", env));
        identity.AddClaim(new Claim("xid", xid));
        identity.AddClaim(new Claim("sig", sig));

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        new ClaimsPrincipal(identity).WriteTo(writer);
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string ComputeSignature(string name, string exp, string env, string xid)
    {
        var input = Encoding.UTF8.GetBytes(string.Concat(name, exp, env, xid));
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }

    private static string? DecodeJwtClaim(string jwt, string claimName)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return null;

        var payload = parts[1];
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
            case 1: return null;
        }
        payload = payload.Replace('-', '+').Replace('_', '/');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
