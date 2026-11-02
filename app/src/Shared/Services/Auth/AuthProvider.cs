using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Shared.Attributes;
using Shared.Extensions;

namespace Shared.Services.Auth
{
    /// <summary>
    /// A fluent utility class to generate Jwt token using client credentials flow
    /// </summary>
    [GenerateAutomaticInterface]
    public class AuthProvider(IMemoryCache cache, HttpClient httpClient, IConfiguration configuration) : IAuthProvider
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;

        public async Task<string> GetTokenAsync(params string[] scopes)
        {
            Logger.LogInformation("--- GetTokenAsync Invoked ---");

            // Clone the input to prevent mutation
            var scopesCopy = (string[])scopes.Clone();

            Array.Sort(scopesCopy);

            // examples:
            // - "nucleus-nucleus/geisinger priorauth/read"
            // - "nucleus-nucleus/geisinger priorauth/write drug/read"
            // - "nucleus-nucleus/moda member/read"
            // - "banjo-someBanjoScopeHere"
            var cacheKey = string.Join(" ", scopesCopy);

            _cache.TryGetValue<string>(cacheKey, out var currentToken);

            if (currentToken is null || !currentToken.IsValidOAuth())
            {
                Logger.LogDebug("--- GetTokenAsync cache miss ---");

                currentToken = await FetchNewTokenAsync(scopes);

                _cache.Set(cacheKey, currentToken);
            }
            else
            {
                Logger.LogDebug("--- GetTokenAsync cache hit ---");
            }

            return currentToken ?? throw new NullReferenceException(nameof(currentToken));
        }

        private async Task<string> FetchNewTokenAsync(params string[] scopes)
        {
            Logger.LogInformation($"--- FetchNewTokenAsync Invoked --- scopes: {string.Join(',', scopes)} ---");

            var clientId =
                _configuration.GetValue<string>("clientId") ?? throw new NullReferenceException("Required ClientId is null in configuration.");
            var clientSecret =
                _configuration.GetValue<string>("clientSecret")
                ?? throw new NullReferenceException("Required ClientSecret is null in configuration.");
            var tokenEndpoint =
                (
                    _configuration.GetValue<string>("NavitusCognitoDomainURL")
                    ?? throw new NullReferenceException("Required Token Endpoint is null in configuration.")
                ) + "/oauth2/token";

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(
                    new Dictionary<string, string?>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["scope"] = string.Join(" ", scopes!)
                    }
                )
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var accessToken =
                JsonSerializer.Deserialize<TokenResponse>(responseContent)?.AccessToken ?? throw new NullReferenceException("Access Token is Null.");

            Logger.LogDebug("--- FetchNewTokenAsync successful ---");

            return accessToken;
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = default!;
        }
    }
}
