using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceClients;
using Services;
using Shared.AWSHelpers;
using Shared.Extensions;
using Shared.Services.Auth;

namespace DocumentMetadataProcessor
{
    public static class Services
    {
        private static readonly Lazy<ServiceProvider> LazyInstance = new(Build);

        public static ServiceProvider Provider => LazyInstance.Value;

        public static ServiceProvider Init()
        {
            return LazyInstance.Value;
        }

        private static ServiceProvider Build()
        {
            var _services = new ServiceCollection();

            var configurationManager = new ConfigurationManager().AddEnvironmentVariables();
            var configuration = configurationManager.Build();

            var currentRegion = AppHelper.GetExecutionRegion();

            configurationManager.AddAmazonSecretsManager(
                currentRegion,
                configuration.GetValue<string>("SECRET_NAME_UM_APP_CLIENT") ?? throw new Exception("Missing required SECRET_NAME_UM_APP_CLIENT")
            );

            configuration = configurationManager.Build();

            _services.AddSingleton<IConfiguration>(configuration);
            _services.AddDocumentMetadataProcessorServices();
            _services.AddServiceClients();
            _services.AddAuthProvider();
            _services.AddMemoryCache();
            _services.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
            _services.AddSingleton<ISqsRecordHandler, DocumentMetadataSqsRecordHandler>();

            return _services.BuildServiceProvider();
        }
    }
}
