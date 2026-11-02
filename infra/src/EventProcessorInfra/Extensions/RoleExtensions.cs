using Amazon.CDK.AWS.IAM;

namespace UM.Infra.EventProcessors.Extensions;

internal static class RoleExtensions
{
    internal static void AddPolicyForAppClientAccess(this Role role)
    {
        var secretArn = $"arn:aws:secretsmanager:*:*:secret:AppClientUM*";
        var crossAccountRoleArn = $"arn:aws:iam::*:role/*-ApiInfraStack-modernui-access-appclient";
        List<PolicyStatement> appClientAccessPolicyStatements =
        [
            new(
                new PolicyStatementProps()
                {
                    Effect = Effect.ALLOW,
                    Resources = [secretArn],
                    Actions = ["secretsmanager:DescribeSecret", "secretsmanager:GetSecretValue"]
                }
            ),
            new(
                new PolicyStatementProps()
                {
                    Effect = Effect.ALLOW,
                    Resources = [crossAccountRoleArn],
                    Actions = ["sts:AssumeRole"]
                }
            ),
            new(
                new PolicyStatementProps()
                {
                    Effect = Effect.ALLOW,
                    Resources = ["*"],
                    Actions = ["kms:DescribeKey", "kms:Decrypt"],
                    Conditions = new Dictionary<string, object>()
                    {
                        {
                            "ForAnyValue:StringLike",
                            new Dictionary<string, string>() { { "kms:ResourceAliases", "alias/CrossAccountSharedSecretKey" } }
                        }
                    },
                }
            )
        ];

        var policy = new Policy(
            role.Stack,
            $"{role.Node.Id}-appclient-policy",
            new PolicyProps() { Document = new PolicyDocument(new PolicyDocumentProps() { Statements = [.. appClientAccessPolicyStatements] }) }
        );

        role.AttachInlinePolicy(policy);
    }
}
