using Microsoft.Extensions.Configuration;

namespace Shared.Configuration
{
    public class AmazonSecretsManagerConfigurationSource(string region, string secretName, string? keyPrefix = null) : IConfigurationSource
    {
        private readonly string _region = region;
        private readonly string _secretName = secretName;
        private readonly string? _keyPrefix = keyPrefix;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AmazonSecretsManagerConfigurationProvider(_region, _secretName, _keyPrefix);
        }
    }
}
