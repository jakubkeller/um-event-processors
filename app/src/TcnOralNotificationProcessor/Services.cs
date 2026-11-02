using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceClients;
using Services;
using Shared.AWSHelpers;
using Shared.Extensions;
using Shared.Services.Auth;

namespace TcnOralNotificationProcessor;

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

        var rxHistoryEntraSecret = configuration.GetValue<string>("SECRET_NAME_RXHISTORY_ENTRA");
        if (!string.IsNullOrEmpty(rxHistoryEntraSecret))
        {
            configurationManager.AddAmazonSecretsManager(currentRegion, rxHistoryEntraSecret, "MedicationHistory");
        }

        configuration = configurationManager.Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddTcnOralNotificationProcessorServices();
        _services.AddServiceClients();
        _services.AddMedicationHistoryClient(configuration);
        _services.AddAuthProvider();
        _services.AddMemoryCache();
        _services.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
        _services.AddSingleton<ISqsRecordHandler, TcnOralNotificationSqsRecordHandler>();

        return _services.BuildServiceProvider();
    }
}
