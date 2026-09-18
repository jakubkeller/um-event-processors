using Microsoft.Extensions.Configuration;
using Shared.Configuration;

namespace Shared.Extensions
{
    public static class AmazonSecretsManagerExtensions
    {
        public static void AddAmazonSecretsManager(this IConfigurationBuilder configurationBuilder, string region, string secretName, string? keyPrefix = null)
        {
            var configurationSource = new AmazonSecretsManagerConfigurationSource(region, secretName, keyPrefix);

            configurationBuilder.Add(configurationSource);
        }
    }
}
