using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Amazon.XRay.Recorder.Handlers.System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ServiceClients.Navitus.MedicationHistory;
using ServiceClients.Navitus.IntegrationPriorAuth;
using ServiceClients.Navitus;
using ServiceClients.Navitus.Document;
using Shared.AWSHelpers;

namespace ServiceClients
{
    public static class DependencyInjection
    {
        private static readonly Lazy<X509Certificate2> NavitusCaCert = new(LoadNavitusCaCert);
        private static IConfiguration? _configuration;

        /// <summary>
        /// Loads the internal Navitus CA certificate from Secrets Manager via IConfiguration.
        /// Requires the NavitusCaCert secret to be configured and accessible.
        /// </summary>
        private static X509Certificate2 LoadNavitusCaCert()
        {
            var certBase64 =
                _configuration?.GetValue<string>("NavitusCaCert:certificate")
                ?? throw new InvalidOperationException(
                    "NavitusCaCert:certificate not found in configuration. Ensure the NavitusCaCert secret is configured and accessible."
                );

            return new X509Certificate2(Convert.FromBase64String(certBase64));
        }

        /// <summary>
        /// Creates an HttpMessageHandler that trusts the internal Navitus CA for service-to-service calls.
        /// Verifies the server cert chains through the known Navitus CA, and still rejects a
        /// chain-valid certificate issued for the wrong hostname.
        /// </summary>
        private static HttpMessageHandler CreateNavitusTrustedHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;

                // Only tolerate chain-trust errors (the untrusted-root case this handler exists for).
                // Anything else - e.g. RemoteCertificateNameMismatch - is rejected outright.
                if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                    return false;

                // Build a chain with our known CA in the extra store
                using var customChain = new X509Chain();
                customChain.ChainPolicy.ExtraStore.Add(NavitusCaCert.Value);
                customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                if (!customChain.Build(cert!))
                    return false;

                // Ensure our trusted CA is actually in the built chain
                return customChain.ChainElements.Any(x => x.Certificate.Thumbprint == NavitusCaCert.Value.Thumbprint);
            };
            return new HttpClientXRayTracingHandler(handler);
        }

        public static void AddServiceClients(this IServiceCollection services)
        {
            if (AppHelper.IsRunningInLambda())
            {
                services
                    .AddHttpClient<IUMServiceClient, UMServiceClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                    })
                    .AddPolicyHandler(GetRetryPolicyForExponentialBackOff())
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        return new HttpClientXRayTracingHandler(new HttpClientHandler());
                    });

                services
                    .AddHttpClient<IUMDocumentServiceClient, UMDocumentServiceClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                    })
                    .AddPolicyHandler(GetRetryPolicyForExponentialBackOff())
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        return new HttpClientXRayTracingHandler(new HttpClientHandler());
                    });

                services
                    .AddHttpClient<IIntegrationPriorAuthServiceClient, IntegrationPriorAuthServiceClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                    })
                    .AddPolicyHandler(GetRetryPolicyForExponentialBackOff())
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        return new HttpClientXRayTracingHandler(new HttpClientHandler());
                    });
            }
            else
            {
                services
                    .AddHttpClient<IUMServiceClient, UMServiceClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(100);
                    })
                    .AddPolicyHandler(GetRetryPolicyForExponentialBackOff());

                services
                    .AddHttpClient<IUMDocumentServiceClient, UMDocumentServiceClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(100);
                    })
                    .AddPolicyHandler(GetRetryPolicyForExponentialBackOff());

                services
                    .AddHttpClient<IIntegrationPriorAuthServiceClient, IntegrationPriorAuthServiceClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(100);
                    })
                    .AddPolicyHandler(GetRetryPolicyForExponentialBackOff());
            }
        }

        public static void AddMedicationHistoryClient(this IServiceCollection services, IConfiguration configuration)
        {
            _configuration = configuration;
            services.Configure<MedicationHistoryClientOptions>(configuration.GetSection("MedicationHistory"));
            var medHistoryOptions = configuration.GetSection("MedicationHistory").Get<MedicationHistoryClientOptions>();
            var baseUrl = medHistoryOptions?.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return;

            services.AddMemoryCache();
            services.AddHttpClient<IMSAuthTokenProvider, MSAuthTokenProvider>();

            services
                .AddHttpClient<IMedicationHistoryServiceClient, MedicationHistoryServiceClient>(client =>
                {
                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = TimeSpan.FromSeconds(15);
                })
                .AddPolicyHandler(GetRetryPolicyForExponentialBackOff())
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    if (AppHelper.IsRunningInLambda())
                        return CreateNavitusTrustedHandler();
                    return new HttpClientHandler();
                });
        }

        private static Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyForExponentialBackOff()
        {
            // exponential back-off
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
