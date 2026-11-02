using Amazon.XRay.Recorder.Handlers.System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Services.Auth
{
    public static class DependencyInjection
    {
        public static void AddAuthProvider(this IServiceCollection services)
        {
            services
                .AddHttpClient<IAuthProvider, AuthProvider>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientXRayTracingHandler(new HttpClientHandler());
                });
        }
    }
}
