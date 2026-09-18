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
                        return new HttpClientXRayTracingHandler(new HttpClientHandler());
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
