using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace Shared.Configuration
{
    public class AmazonSecretsManagerConfigurationProvider(string region, string secretName, string? keyPrefix = null) : ConfigurationProvider
    {
        private readonly string _region = region;
        private readonly string _secretName = secretName;
        private readonly string? _keyPrefix = keyPrefix;

        public override void Load()
        {
            var secret = GetSecret();

            var data = JsonSerializer.Deserialize<Dictionary<string, string?>>(secret) ?? throw new Exception("Unable to retrieve required secret");

            Data = string.IsNullOrEmpty(_keyPrefix)
                ? data
                : data.ToDictionary(kvp => $"{_keyPrefix}:{kvp.Key}", kvp => kvp.Value);
        }

        private string GetSecret()
        {
            var request = new GetSecretValueRequest
            {
                SecretId = _secretName,
                VersionStage = "AWSCURRENT" // VersionStage defaults to AWSCURRENT if unspecified.
            };

            using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(_region));
            var response = client.GetSecretValueAsync(request).Result;

            string secretString;
            if (response.SecretString != null)
            {
                secretString = response.SecretString;
            }
            else
            {
                var memoryStream = response.SecretBinary;
                var reader = new StreamReader(memoryStream);
                secretString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
            }

            return secretString;
        }
    }
}
