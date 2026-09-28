using Amazon;
using Amazon.CDK;
using UM.Infra.Base.Helpers;

namespace UM.Infra.EventProcessors;

sealed class Program
{
    private static readonly string LOCAL_APP_ROOT = System.Environment.GetEnvironmentVariable("LOCAL_APP_ROOT") ?? "um";
    private static readonly string API_VERSION = InfraConfigHelper.GetApiVersion();

    public static void Main(string[] args)
    {
        var app = new App();

        _ = new EventProcessorInfraStack(
            app,
            $"{LOCAL_APP_ROOT}-{API_VERSION}-EventProcessorInfraStack",
            new EventProcessorInfraStackProps
            {
                ApiVersion = API_VERSION,
                Env = EnvironmentHelper.MakeEnvironment(),
            }
        );

        app.Synth();
    }
}
