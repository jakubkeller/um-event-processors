using Amazon.CDK;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace UM.Infra.EventProcessors.Constructs;

public class ReportingBucketProps
{
    /// <summary>
    /// Optional existing KMS key alias to encrypt the bucket with. When null, the shared SQS key
    /// (alias/SQSKey) is reused to satisfy the "use existing KMS" requirement.
    /// </summary>
    public string? EncryptionKeyAlias { get; set; }

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
        EncryptionKey = Key.FromLookup(
            this,
            "ReportingBucketKey",
            new KeyLookupOptions() { AliasName = props.EncryptionKeyAlias ?? "alias/SQSKey" }
        );

        Bucket = new Bucket(
            this,
            "Bucket",
            new BucketProps()
            {
                Encryption = BucketEncryption.KMS,
                EncryptionKey = EncryptionKey,
                EnforceSSL = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                Versioned = false,
                RemovalPolicy = props.IsSandbox ? RemovalPolicy.DESTROY : RemovalPolicy.RETAIN,
                AutoDeleteObjects = props.IsSandbox,
            }
        );
    }
}
