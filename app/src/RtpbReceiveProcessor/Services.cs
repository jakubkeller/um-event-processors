using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services;

namespace RtpbReceiveProcessor
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

            _services.AddSingleton<IConfiguration>(configuration);
            _services.AddRtpbReceiveProcessorServices();
            _services.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
            _services.AddSingleton<ISqsRecordHandler, RtpbReceiveSqsRecordHandler>();

            return _services.BuildServiceProvider();
        }
    }
}
