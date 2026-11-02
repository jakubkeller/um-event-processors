using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Services.Implementation;
using Services.Interface;

namespace Services
{
    public static class DependencyInjection
    {
        public static void AddTemplatedNoteProcessorServices(this IServiceCollection services)
        {
            services.AddSingleton<ITemplatedNoteService, TemplatedNoteService>();
        }

        public static void AddDocumentMetadataProcessorServices(this IServiceCollection services)
        {
            services.AddSingleton<IDocumentService, DocumentService>();
        }

        public static void AddSendEmailProcessorServices(this IServiceCollection services)
        {
            services.AddSingleton<IFileService, FileService>();

            services.AddSingleton<GraphServiceClient>(sp =>
            {
                var creds = sp.GetRequiredService<IOptions<GraphServiceClientCredentials>>().Value;

                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var credential = new ClientSecretCredential(
                    creds.TenantId,
                    creds.ClientId,
                    creds.Secret,
                    options
                );

                string[] scopes = { "https://graph.microsoft.com/.default" };
                return new GraphServiceClient(
                    credential,
                    scopes
                );
            });
            services.AddScoped<IGraphEmailClient, GraphEmailClient>();
            services.AddSingleton<ISendEmailService, SendEmailService>();
        }

        public static void AddEmailMetadataProcessorServices(this IServiceCollection services)
        {
            services.AddSingleton<IEmailService, EmailService>();
        }

        public static void AddTcnOralNotificationProcessorServices(this IServiceCollection services)
        {
            services.AddSingleton<ITcnOralNotificationService, TcnOralNotificationService>();
        }

        public static void AddTcnPollingProcessorServices(this IServiceCollection services)
        {
            services.AddSingleton<ITcnPollingService, TcnPollingService>();
        }
    }
}
