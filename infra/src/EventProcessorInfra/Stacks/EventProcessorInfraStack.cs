using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SSM;
using Constructs;
using UM.Infra.Base;
using UM.Infra.Base.Config;
using UM.Infra.EventProcessors.Constructs;
using Policy = Amazon.CDK.AWS.IAM.Policy;

namespace UM.Infra.EventProcessors;

public class EventProcessorInfraStackProps : StackProps
{
    public string ApiVersion { get; set; } = default!;
}

public class EventProcessorInfraStack : BaseStack
{
    internal EventProcessorInfraStack(Construct scope, string id, EventProcessorInfraStackProps props)
        : base(scope, id, props, forceDisableNagPack: true)
    {
        var ssmPathRoot = $"/{AppRoot}/api/{props.ApiVersion}";
        var infraConfig = InfraConfigHelper.GetCurrentInfraEnvironment(CurrentEnvironment.Name);
        var isSandbox = CurrentEnvironment.Type.Equals(EnvironmentType.Sandbox);
        var source = $"com.navitus.{AppRoot}";

        var apiNetworkingSSMPathRoot = "/um/api/networking";
        var umLambdasSG = SecurityGroup.FromSecurityGroupId(
            this,
            "UMLambdasSG",
            StringParameter.ValueForStringParameter(this, $"{apiNetworkingSSMPathRoot}/UMLambdasSGId")
        );

        var umEventBus = Amazon.CDK.AWS.Events.EventBus.FromEventBusArn(
            this,
            "UMEventBus",
            StringParameter.FromStringParameterName(this, "UMEventBusArn", "/um/core/UMEventBusArn").StringValue
        );

        var umApiCustomBaseURL = StringParameter
            .FromStringParameterName(this, "UMApiCustomBaseURLParam", $"{ssmPathRoot}/UMApiCustomBaseURL")
            .StringValue;

        var databaseCredentialSet = new HashSet<string>();
        foreach (var clientConfiguration in infraConfig.ClientConfigurations)
            databaseCredentialSet.Add(clientConfiguration.SecretNameUtilizationManagementDatabase);

        AddTemplatedNoteProcessor(id, infraConfig, isSandbox, source, umEventBus, umApiCustomBaseURL, databaseCredentialSet, [umLambdasSG]);
        AddEmailProcessor(id, infraConfig, isSandbox, umEventBus, umApiCustomBaseURL, databaseCredentialSet, [umLambdasSG]);
        AddSendEmailProcessor(id, infraConfig, isSandbox, source, umEventBus, umApiCustomBaseURL, databaseCredentialSet, [umLambdasSG]);
        AddTcnOralNotificationProcessor(id, infraConfig, isSandbox, source, umEventBus, umApiCustomBaseURL, [umLambdasSG]);
        AddTcnPollingProcessor(id, infraConfig, isSandbox, umApiCustomBaseURL, [umLambdasSG]);

        _ = new EventProcessor(
            this,
            "DocumentMetadataProcessor",
            new EventProcessorProps()
            {
                UMApiCustomBaseURL = umApiCustomBaseURL,
                AppRoot = AppRoot,
                DatabaseCredentialSet = databaseCredentialSet,
                InfraConfig = infraConfig,
                IsSandbox = isSandbox,
                LambdaAssetPath = "../dist/event-processors/DocumentMetadataProcessor/release/publish/",
                LambdaHandlerName = "DocumentMetadataProcessor::DocumentMetadataProcessor.Function::FunctionHandler",
                Source = "com.navitus.integration.priorauth",
                SourceEventNames = ["OutboundMailReceived", "OutboundFaxReceived"],
                UMEventBus = umEventBus,
                SecurityGroups = [umLambdasSG]
            }
        );
    }

    private void AddTemplatedNoteProcessor(
        string id,
        Environment infraConfig,
        bool isSandbox,
        string source,
        IEventBus umEventBus,
        string umApiCustomBaseURL,
        HashSet<string> databaseCredentialSet,
        ISecurityGroup[]? securityGroups
    )
    {
        var lambda = new FunctionWithRole(
            this,
            "templatedNoteHandlerLambda",
            new FunctionWithRoleProps()
            {
                IsSandbox = isSandbox,
                AssetPath = "../dist/event-processors/TemplatedNoteProcessor/release/publish/",
                Handler = "TemplatedNoteProcessor::TemplatedNoteProcessor.Function::FunctionHandler",
                Environment = new Dictionary<string, string>()
                {
                    { "NavitusUtilizationManagementBaseURL", umApiCustomBaseURL },
                    { "SECRET_NAME_UM_APP_CLIENT", infraConfig.UMAppClientSecretArn },
                    { "NavitusCognitoDomainURL", infraConfig.CognitoDomainURL }
                },
                MemorySize = 256,
                VpcId = infraConfig.VpcId,
                Timeout = 120,
                AllowAppClientAccess = true,
                AppRoot = AppRoot,
                SecurityGroups = securityGroups,
            }
        );

        var queue = new QueueWithDLQ(
            this,
            "templatedNoteHandlerQueue",
            new QueueWithDLQProps()
            {
                Fifo = false,
                MaxReceiveCount = isSandbox ? 1 : 10,
                VisibilityTimeoutSeconds = isSandbox ? 120 : 900,
                RetentionPeriodDays = 14,
            }
        );

        queue.MainQueue.GrantConsumeMessages(lambda.Role);

        lambda.LambdaFunction.AddEventSource(
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

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowSSMGetAppParameters",
                    Effect = Effect.ALLOW,
                    Actions = ["ssm:GetParameter", "ssm:GetParametersByPath"],
                    Resources =
                    [
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/{AppRoot}/api*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/db*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/core*",
                        .. databaseCredentialSet.Select(secret =>
                            $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/aws/reference/secretsmanager/{secret}"
                        ),
                    ]
                }
            )
        );

        _ = new Rule(
            this,
            "TemplatedNoteCreatedEventRule",
            new RuleProps()
            {
                Enabled = true,
                EventBus = umEventBus,
                Targets =
                [
                    new SqsQueue(
                        queue.MainQueue,
                        new SqsQueueProps() { MessageGroupId = queue.MainQueue.Fifo ? id : null }
                    ),
                ],
                Description = "Rule to handle Templated Note Created Notifications",
                EventPattern = new EventPattern
                {
                    Source = [source],
                    DetailType = ["EpisodeCreated", "EpisodeUpdatedV2", "GrievanceAndAppealsEpisodeCreated", "GrievanceAndAppealsEpisodeUpdated"]
                }
            }
        );
    }

    private void AddEmailProcessor(
        string id,
        Environment infraConfig,
        bool isSandbox,
        IEventBus umEventBus,
        string umApiCustomBaseURL,
        HashSet<string> databaseCredentialSet,
        ISecurityGroup[]? securityGroups
    )
    {
        var lambda = new FunctionWithRole(
            this,
            "emailHandlerLambda",
            new FunctionWithRoleProps()
            {
                IsSandbox = isSandbox,
                AssetPath = "../dist/event-processors/EmailProcessor/release/publish/",
                Handler = "EmailProcessor::EmailProcessor.Function::FunctionHandler",
                Environment = new Dictionary<string, string>()
                {
                    { "NavitusUtilizationManagementBaseURL", umApiCustomBaseURL },
                    { "SECRET_NAME_UM_APP_CLIENT", infraConfig.UMAppClientSecretArn },
                    { "NavitusCognitoDomainURL", infraConfig.CognitoDomainURL }
                },
                MemorySize = 256,
                VpcId = infraConfig.VpcId,
                Timeout = 120,
                AllowAppClientAccess = true,
                AppRoot = AppRoot,
                SecurityGroups = securityGroups,
            }
        );

        var queue = new QueueWithDLQ(
            this,
            "emailHandlerQueue",
            new QueueWithDLQProps()
            {
                Fifo = false,
                MaxReceiveCount = isSandbox ? 1 : 10,
                VisibilityTimeoutSeconds = isSandbox ? 120 : 900,
                RetentionPeriodDays = 14,
            }
        );

        queue.MainQueue.GrantConsumeMessages(lambda.Role);

        lambda.LambdaFunction.AddEventSource(
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

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowSSMGetAppParameters",
                    Effect = Effect.ALLOW,
                    Actions = ["ssm:GetParameter", "ssm:GetParametersByPath"],
                    Resources =
                    [
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/{AppRoot}/api*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/db*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/core*",
                        .. databaseCredentialSet.Select(secret =>
                            $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/aws/reference/secretsmanager/{secret}"
                        ),
                    ]
                }
            )
        );

        _ = new Rule(
            this,
            "OutboundEmailReceivedEventRule",
            new RuleProps()
            {
                Enabled = true,
                EventBus = umEventBus,
                Targets =
                [
                    new SqsQueue(queue.MainQueue, new SqsQueueProps() { MessageGroupId = queue.MainQueue.Fifo ? id : null }),
                ],
                Description = "Rule to handle Outbound Email Received Notifications",
                EventPattern = new EventPattern { Source = ["com.navitus.integration.priorauth"], DetailType = ["OutboundEmailReceived"] }
            }
        );
    }

    private void AddSendEmailProcessor(
        string id,
        Environment infraConfig,
        bool isSandbox,
        string source,
        IEventBus umEventBus,
        string umApiCustomBaseURL,
        HashSet<string> databaseCredentialSet,
        ISecurityGroup[]? securityGroups
    )
    {
        var lambda = new FunctionWithRole(
            this,
            "SendEmailHandlerLambda",
            new FunctionWithRoleProps()
            {
                IsSandbox = isSandbox,
                AssetPath = "../dist/event-processors/SendEmailProcessor/release/publish/",
                Handler = "SendEmailProcessor::SendEmailProcessor.Function::FunctionHandler",
                Environment = new Dictionary<string, string>()
                {
                    { "NavitusUtilizationManagementBaseURL", umApiCustomBaseURL },
                    { "SECRET_NAME_UM_APP_CLIENT", infraConfig.UMAppClientSecretArn },
                    { "SECRET_NAME_UM_GRAPH_CLIENT", infraConfig.UMGraphClientSecretArn },
                    { "GRAPH_CLIENT_FROM_ADDRESS", infraConfig.UMGraphClientFromAddress },
                    { "GRAPH_CLIENT_USER", infraConfig.UMGraphClientUser },
                    { "NavitusCognitoDomainURL", infraConfig.CognitoDomainURL }
                },
                MemorySize = 256,
                VpcId = infraConfig.VpcId,
                Timeout = 120,
                AllowAppClientAccess = true,
                AppRoot = AppRoot,
                SecurityGroups = securityGroups,
            }
        );

        var queue = new QueueWithDLQ(
            this,
            "SendEmailHandlerQueue",
            new QueueWithDLQProps()
            {
                Fifo = false,
                MaxReceiveCount = isSandbox ? 1 : 10,
                VisibilityTimeoutSeconds = isSandbox ? 120 : 900,
                RetentionPeriodDays = 14,
            }
        );

        queue.MainQueue.GrantConsumeMessages(lambda.Role);

        lambda.LambdaFunction.AddEventSource(
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

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowSSMGetAppParameters",
                    Effect = Effect.ALLOW,
                    Actions = ["ssm:GetParameter", "ssm:GetParametersByPath"],
                    Resources =
                    [
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/{AppRoot}/api*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/db*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/core*",
                        .. databaseCredentialSet.Select(secret =>
                            $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/aws/reference/secretsmanager/{secret}"
                        ),
                    ]
                }
            )
        );

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Sid = "AllowGraphSecretRead",
                    Effect = Effect.ALLOW,
                    Actions = ["secretsmanager:GetSecretValue", "secretsmanager:DescribeSecret"],
                    Resources = [infraConfig.UMGraphClientSecretArn]
                }
            )
        );

        _ = new Rule(
            this,
            "SendEmailCreatedEventRule",
            new RuleProps()
            {
                Enabled = true,
                EventBus = umEventBus,
                Targets =
                [
                    new SqsQueue(
                        queue.MainQueue,
                        new SqsQueueProps() { MessageGroupId = queue.MainQueue.Fifo ? id : null }
                    ),
                ],
                Description = "Rule to handle sending outbound email Notifications",
                EventPattern = new EventPattern { Source = [source], DetailType = ["SendEmail"] }
            }
        );
    }

    private void AddTcnOralNotificationProcessor(
        string id,
        Environment infraConfig,
        bool isSandbox,
        string source,
        IEventBus umEventBus,
        string umApiCustomBaseURL,
        ISecurityGroup[]? securityGroups
    )
    {
        var lambda = new FunctionWithRole(
            this,
            "TcnOralNotificationLambda",
            new FunctionWithRoleProps()
            {
                IsSandbox = isSandbox,
                AssetPath = "../dist/event-processors/TcnOralNotificationProcessor/release/publish/",
                Handler = "TcnOralNotificationProcessor::TcnOralNotificationProcessor.Function::FunctionHandler",
                Environment = new Dictionary<string, string>()
                {
                    { "NavitusUtilizationManagementBaseURL", umApiCustomBaseURL },
                    { "IntegrationPriorAuthBaseURL", infraConfig.IntegrationPriorAuthBaseURL },
                    { "SECRET_NAME_UM_APP_CLIENT", infraConfig.UMAppClientSecretArn },
                    { "NavitusCognitoDomainURL", infraConfig.CognitoDomainURL },
                    { "TCN_SCHEDULE_TEMPLATE", infraConfig.TcnScheduleTemplateNumber },
                    { "TCN_IMPORT_TEMPLATE", infraConfig.TcnImportTemplateNumber },
                    { "TCN_DESCRIPTION", infraConfig.TcnDescription },
                    { "TCN_TIMEZONE", infraConfig.TcnTimezone },
                    { "TCN_DIAL_ORDER", infraConfig.TcnDialOrder },
                    { "MedicationHistory__BaseUrl", infraConfig.MedicationHistoryBaseUrl ?? string.Empty },
                    { "MedicationHistory__EntraTokenUrl", infraConfig.MedicationHistoryEntraTokenUrl ?? string.Empty },
                    { "MedicationHistory__MsAuthEnv", infraConfig.MedicationHistoryMsAuthEnv },
                    { "SECRET_NAME_RXHISTORY_ENTRA", infraConfig.RxHistoryEntraCredentialsSecretName ?? string.Empty },
                },
                SecretsToGrantRead = string.IsNullOrEmpty(infraConfig.RxHistoryEntraCredentialsSecretName) ? null : [infraConfig.RxHistoryEntraCredentialsSecretName],
                MemorySize = 256,
                VpcId = infraConfig.VpcId,
                Timeout = 120,
                AllowAppClientAccess = true,
                AppRoot = AppRoot,
                SecurityGroups = securityGroups,
            }
        );

        var queue = new QueueWithDLQ(
            this,
            "TcnOralNotificationQueue",
            new QueueWithDLQProps()
            {
                Fifo = false,
                MaxReceiveCount = isSandbox ? 1 : 10,
                VisibilityTimeoutSeconds = isSandbox ? 120 : 900,
                RetentionPeriodDays = 14,
            }
        );

        queue.MainQueue.GrantConsumeMessages(lambda.Role);

        lambda.LambdaFunction.AddEventSource(
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

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowSSMGetAppParameters",
                    Effect = Effect.ALLOW,
                    Actions = ["ssm:GetParameter", "ssm:GetParametersByPath"],
                    Resources =
                    [
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/{AppRoot}/api*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/core*",
                    ]
                }
            )
        );

        _ = new Rule(
            this,
            "TcnOralNotificationEventRule",
            new RuleProps()
            {
                Enabled = true,
                EventBus = umEventBus,
                Targets =
                [
                    new SqsQueue(
                        queue.MainQueue,
                        new SqsQueueProps() { MessageGroupId = queue.MainQueue.Fifo ? id : null }
                    ),
                ],
                Description = "Rule to route TcnInformationEvent events to TcnOralNotificationProcessor",
                EventPattern = new EventPattern { Source = [source], DetailType = ["TcnInformationEvent"] }
            }
        );
    }

    private void AddTcnPollingProcessor(
        string id,
        Environment infraConfig,
        bool isSandbox,
        string umApiCustomBaseURL,
        ISecurityGroup[]? securityGroups
    )
    {
        var lambda = new FunctionWithRole(
            this,
            "TcnPollingLambda",
            new FunctionWithRoleProps()
            {
                IsSandbox = isSandbox,
                AssetPath = "../dist/event-processors/TcnPollingProcessor/release/publish/",
                Handler = "TcnPollingProcessor::TcnPollingProcessor.Function::FunctionHandler",
                Environment = new Dictionary<string, string>()
                {
                    { "NavitusUtilizationManagementBaseURL", umApiCustomBaseURL },
                    { "IntegrationPriorAuthBaseURL", infraConfig.IntegrationPriorAuthBaseURL },
                    { "SECRET_NAME_UM_APP_CLIENT", infraConfig.UMAppClientSecretArn },
                    { "NavitusCognitoDomainURL", infraConfig.CognitoDomainURL },
                    { "UMEventBusArn", infraConfig.UMEventBusArn },
                    { "TcnFailureNotificationDistributionList", infraConfig.TcnFailureNotificationDistributionList },
                },
                MemorySize = 256,
                VpcId = infraConfig.VpcId,
                Timeout = 300,
                AllowAppClientAccess = true,
                AppRoot = AppRoot,
                SecurityGroups = securityGroups,
            }
        );

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowSSMGetAppParameters",
                    Effect = Effect.ALLOW,
                    Actions = ["ssm:GetParameter", "ssm:GetParametersByPath"],
                    Resources =
                    [
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/{AppRoot}/api*",
                        $"arn:{Aws.PARTITION}:ssm:{Aws.REGION}:{Aws.ACCOUNT_ID}:parameter/um/core*",
                    ]
                }
            )
        );

        lambda.Role.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Sid = "AllowEventBridgePutEvents",
                    Effect = Effect.ALLOW,
                    Actions = ["events:PutEvents"],
                    Resources = [infraConfig.UMEventBusArn]
                }
            )
        );

        _ = new Rule(
            this,
            "TcnPollingScheduleRule",
            new RuleProps()
            {
                Enabled = true,
                Schedule = Schedule.Rate(Duration.Minutes(15)),
                Description = "Triggers TcnPollingProcessor every 15 minutes to poll for TCN call results",
                Targets = [new LambdaFunction(lambda.LambdaFunction, new LambdaFunctionProps() { RetryAttempts = 0 })]
            }
        );
    }
}
