using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using UM.Infra.EventProcessors.Extensions;

namespace UM.Infra.EventProcessors.Constructs;

public class FunctionWithRoleProps
{
    public string AssetPath { get; set; } = default!;
    public string Handler { get; set; } = default!;
    public int? Timeout { get; set; }
    public int? MemorySize { get; set; }
    public Dictionary<string, string>? Environment { get; set; }
    public string[] SecretsToGrantRead { get; set; } = default!;
    public string VpcId { get; set; } = default!;
    public bool IsSandbox { get; set; }
    public bool AllowAppClientAccess { get; set; } = false;
    public string AppRoot { get; set; } = default!;
    public ISecurityGroup[]? SecurityGroups { get; set; }
}

public class FunctionWithRole : Construct
{
    internal Role Role { get; set; }
    internal Function LambdaFunction { get; set; }

    public FunctionWithRole(Construct scope, string id, FunctionWithRoleProps props)
        : base(scope, id)
    {
        var roleProps = new RoleProps() { AssumedBy = new ServicePrincipal("lambda.amazonaws.com") };
        if (props.AllowAppClientAccess)
            roleProps.RoleName = $"{props.AppRoot}-{id}-role-{Constants.RoleNameKeywordForAppClientAccess}";

        Role = new Role(this, $"{id}-Role", roleProps);

        if (props.AllowAppClientAccess)
            Role.AddPolicyForAppClientAccess();

        Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowCloudwatchLogs",
                    Effect = Effect.ALLOW,
                    Actions = ["logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents"],
                    Resources = ["*"]
                }
            )
        );

        Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowXRayAccess",
                    Effect = Effect.ALLOW,
                    Actions =
                    [
                        "xray:PutTraceSegments",
                        "xray:PutTelemetryRecords",
                        "xray:GetSamplingRules",
                        "xray:GetSamplingTargets",
                        "xray:GetSamplingStatisticSummaries"
                    ],
                    Resources = ["*"]
                }
            )
        );

        (props.SecretsToGrantRead ?? []).ToList().ForEach(secret => GrantSecretReadAccess(secret, Role));

        Role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole"));

        var environment = props.Environment ?? [];
        environment.Add("POWERTOOLS_SERVICE_NAME", id);
        environment.Add("POWERTOOLS_METRICS_NAMESPACE", "UMApi");
        environment.Add("POWERTOOLS_TRACER_CAPTURE_HTTPS_REQUESTS", props.IsSandbox ? "true" : "");
        environment.Add("POWERTOOLS_LOGGER_LOG_EVENT", props.IsSandbox ? "true" : "");
        environment.Add("POWERTOOLS_LOG_LEVEL", props.IsSandbox ? "Debug" : "Information");
        environment.Add("AWS_LAMBDA_DOTNET_PREJIT", "always");

        var vpc = Vpc.FromLookup(this, props.VpcId, new VpcLookupOptions { VpcId = props.VpcId });

        LambdaFunction = new Function(
            this,
            "Function",
            new FunctionProps()
            {
                Role = Role,
                Architecture = Architecture.X86_64,
                Code = Code.FromAsset(props.AssetPath),
                Handler = props.Handler,
                Runtime = Runtime.DOTNET_8,
                Timeout = Duration.Seconds(props.Timeout ?? 29),
                MemorySize = props.MemorySize ?? 512,
                Environment = environment,
                Tracing = Tracing.ACTIVE,
                Vpc = vpc,
                SecurityGroups = props.SecurityGroups
            }
        );
    }

    private void GrantSecretReadAccess(string secretId, Role role)
    {
        var secret = Secret.FromSecretNameV2(this, $"{secretId}Grant", secretId);
        secret.GrantRead(role);
    }
}
