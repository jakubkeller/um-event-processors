using System.Reflection;
using System.Text.Json;

namespace UM.Infra.EventProcessors;

public class InfraConfig
{
    public string ApiVersion { get; set; } = "v0";
    public List<Environment> Environments { get; set; } = [];
}

public class Environment
{
    public string Name { get; set; } = default!;
    public string UMAppClientSecretArn { get; set; } = default!;
    public string UMGraphClientSecretArn { get; set; } = default!;
    public string UMGraphClientFromAddress { get; set; } = default!;
    public string UMGraphClientUser { get; set; } = default!;
    public string CognitoDomainURL { get; set; } = default!;
    public string UserPoolARN { get; set; } = default!;
    public string VpcId { get; set; } = default!;
    public string HostedZone { get; set; } = default!;
    public string HostedZoneId { get; set; } = default!;
    public string ModernUiAccountId { get; set; } = default!;
    public int LambdaMinConcurrency { get; set; }
    public int LambdaMaxConcurrency { get; set; }
    public double LambdaConcurrencyScalingPolicyTargetValue { get; set; }
    public ClientConfiguration[] ClientConfigurations { get; set; } = default!;
    public string IntegrationPriorAuthBaseURL { get; set; } = default!;
    public string TcnScheduleTemplateNumber { get; set; } = default!;
    public string TcnImportTemplateNumber { get; set; } = default!;
    public string TcnDescription { get; set; } = default!;
    public string TcnTimezone { get; set; } = default!;
    public string TcnDialOrder { get; set; } = default!;
    public bool TcnEnabled { get; set; } = true;
    public string UMEventBusArn { get; set; } = default!;
    public string TcnFailureNotificationDistributionList { get; set; } = default!;
    public string? MedicationHistoryBaseUrl { get; set; }
    public string? MedicationHistoryEntraTokenUrl { get; set; }
    public string MedicationHistoryMsAuthEnv { get; set; } = "Test";
    public string? RxHistoryEntraCredentialsSecretName { get; set; }
}

public class ClientConfiguration
{
    public string ClientName { get; set; } = default!;
    public string SecretNameUtilizationManagementDatabase { get; set; } = default!;
    public string ClientId { get; set; } = default!;
}

public static class InfraConfigHelper
{
    public static InfraConfig GetInfraConfig()
    {
        string assemblyDirectoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        string configFilePath = Path.Combine(assemblyDirectoryPath, "infraconfig.json");
        var config = JsonSerializer.Deserialize<InfraConfig>(File.ReadAllText(configFilePath));
        return config ?? throw new Exception("Could not load infra config.");
    }

    public static Environment GetCurrentInfraEnvironment(string environmentName)
    {
        var config = GetInfraConfig();
        return config.Environments.FirstOrDefault(x => x.Name.Equals(environmentName))
            ?? throw new Exception($"Could not find config with '{environmentName}' environment.");
    }

    public static string GetApiVersion()
    {
        return GetInfraConfig().ApiVersion;
    }
}
