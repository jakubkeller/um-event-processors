using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda.EventSources;
using Constructs;

namespace UM.Infra.EventProcessors.Constructs;

internal class EventProcessorProps
{
    internal Environment InfraConfig { get; set; } = default!;
    internal bool IsSandbox { get; set; }
    internal string Source { get; set; } = default!;
    internal IEventBus UMEventBus { get; set; } = default!;
    internal string UMApiCustomBaseURL { get; set; } = default!;
    internal HashSet<string> DatabaseCredentialSet { get; set; } = default!;
    internal string AppRoot { get; set; } = default!;
    internal string[] SourceEventNames { get; set; } = default!;
    internal string LambdaAssetPath { get; set; } = default!;
    internal string LambdaHandlerName { get; set; } = default!;
    public ISecurityGroup[]? SecurityGroups { get; set; }
}

internal class EventProcessor : Construct
{
    internal QueueWithDLQ QueueWithDLQ { get; private set; }

    internal EventProcessor(Construct scope, string id, EventProcessorProps props)
        : base(scope, id)
    {
        var eventProcessorLambda = new FunctionWithRole(
            this,
            id,
            new FunctionWithRoleProps()
            {
                IsSandbox = props.IsSandbox,
                AssetPath = props.LambdaAssetPath,
                Handler = props.LambdaHandlerName,
                Environment = new Dictionary<string, string>()
                {
                    { "NavitusUtilizationManagementBaseURL", props.UMApiCustomBaseURL },
                    { "SECRET_NAME_UM_APP_CLIENT", props.InfraConfig.UMAppClientSecretArn },
                    { "NavitusCognitoDomainURL", props.InfraConfig.CognitoDomainURL }
                },
                MemorySize = 256,
                VpcId = props.InfraConfig.VpcId,
                Timeout = 120,
                AllowAppClientAccess = true,
                AppRoot = props.AppRoot,
                SecurityGroups = props.SecurityGroups,
            }
        );

        var queue = new QueueWithDLQ(
            this,
            "Queue",
            new QueueWithDLQProps()
            {
                Fifo = false,
                MaxReceiveCount = props.IsSandbox ? 1 : 10,
                VisibilityTimeoutSeconds = props.IsSandbox ? 120 : 900,
                RetentionPeriodDays = 14,
            }
        );
        QueueWithDLQ = queue;

        queue.MainQueue.GrantConsumeMessages(eventProcessorLambda.Role);

        eventProcessorLambda.LambdaFunction.AddEventSource(
            new SqsEventSource(
                queue.MainQueue,
                new SqsEventSourceProps()
                {
                    BatchSize = 10,
                    ReportBatchItemFailures = true,
                    MaxBatchingWindow = Duration.Seconds(1),
                    MaxConcurrency = 50,
                }
            )
        );

        eventProcessorLambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowSSMGetAppParameters",
                    Effect = Effect.ALLOW,
                    Actions = ["ssm:GetParameter", "ssm:GetParametersByPath"],
                    Resources =
                    [
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/{props.AppRoot}/api*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/db*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/core*",
                        .. props.DatabaseCredentialSet.Select(secret =>
                            $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/aws/reference/secretsmanager/{secret}"
                        ),
                    ]
                }
            )
        );

        _ = new Rule(
            this,
            "Rule",
            new RuleProps()
            {
                Enabled = true,
                EventBus = props.UMEventBus,
                Targets = [new SqsQueue(queue.MainQueue, new SqsQueueProps() { MessageGroupId = queue.MainQueue.Fifo ? id : null })],
                Description = $"Rule to handle {id} Notifications",
                EventPattern = new EventPattern { Source = [props.Source], DetailType = props.SourceEventNames }
            }
        );
    }
}
