using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace UM.Infra.EventProcessors.Constructs;

public class ReportingBucketProps
{
    public string BucketName { get; set; } = default!;

    // Cross-account reader roles granted read access to the event prefixes.
    public string[] ReaderRoleArns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// When true (sandbox), the bucket auto-deletes objects and is destroyed on stack delete.
    /// </summary>
    public bool IsSandbox { get; set; }
}

/// <summary>
/// S3 bucket for RTPB reporting (PBI 292733). Stores one JSON object per RTPB transaction,
/// partitioned yyyy/MM/dd/HH. Read by the Data Lakehouse. Long retention; KMS-encrypted; TLS-only;
/// public access fully blocked.
/// </summary>
public class ReportingBucket : Construct
{
    internal IKey EncryptionKey { get; private set; }
    internal Bucket Bucket { get; private set; }

    public ReportingBucket(Construct scope, string id, ReportingBucketProps props)
        : base(scope, id)
    {
        EncryptionKey = Alias.FromAliasName(this, "OrgS3Key", "alias/S3BucketDefaultKMSKey");

        Bucket = new Bucket(
            this,
            "Bucket",
            new BucketProps()
            {
                BucketName = props.BucketName,
                Encryption = BucketEncryption.KMS,
                EncryptionKey = EncryptionKey,
                BucketKeyEnabled = true,
                EnforceSSL = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                PublicReadAccess = false,
                Versioned = true,
                RemovalPolicy = props.IsSandbox ? RemovalPolicy.DESTROY : RemovalPolicy.RETAIN,
                AutoDeleteObjects = props.IsSandbox,
            }
        );

        if (props.ReaderRoleArns.Length > 0)
        {
            var principals = props.ReaderRoleArns.Select(arn => (IPrincipal)new ArnPrincipal(arn)).ToArray();

            Bucket.AddToResourcePolicy(
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Principals = principals,
                        Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
                        Resources = [Bucket.BucketArn],
                    }
                )
            );
            Bucket.AddToResourcePolicy(
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Principals = principals,
                        Actions = ["s3:GetObject", "s3:GetObjectVersion"],
                        Resources = [$"{Bucket.BucketArn}/*"],
                    }
                )
            );
        }
    }
}
