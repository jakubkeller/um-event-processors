using Amazon.CDK;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace UM.Infra.EventProcessors.Constructs;

public class QueueWithDLQProps
{
    public bool Fifo { get; set; }
    public double? RetentionPeriodDays { get; set; }
    public double? VisibilityTimeoutSeconds { get; set; }
    public double? MaxReceiveCount { get; set; }
}

public class QueueWithDLQ : Construct
{
    internal IKey EncryptionKey { get; private set; }
    internal IQueue MainQueue { get; private set; }
    internal IQueue DeadLetterQueue { get; private set; }

    public QueueWithDLQ(Construct scope, string id, QueueWithDLQProps props)
        : base(scope, id)
    {
        if (props.RetentionPeriodDays == null || props.RetentionPeriodDays < 1 || props.RetentionPeriodDays > 14)
            throw new ArgumentException("RetentionPeriodDays must be between 1 and 14 (inclusive).");

        if (props.VisibilityTimeoutSeconds != null && props.VisibilityTimeoutSeconds < 1)
            throw new ArgumentException("VisibilityTimeoutSeconds when specified must be greater than 1.");

        EncryptionKey = Key.FromLookup(this, "SQS", new KeyLookupOptions() { AliasName = "alias/SQSKey" });

        DeadLetterQueue = new Queue(
            this,
            "DeadLetterQueue",
            new QueueProps()
            {
                Encryption = QueueEncryption.KMS,
                EncryptionMasterKey = EncryptionKey,
                Fifo = props.Fifo ? props.Fifo : null,
                EnforceSSL = true,
                RetentionPeriod = Duration.Days(14),
            }
        );

        MainQueue = new Queue(
            this,
            "MainQueue",
            new QueueProps()
            {
                DeadLetterQueue = new DeadLetterQueue() { Queue = DeadLetterQueue, MaxReceiveCount = props.MaxReceiveCount ?? 3 },
                Encryption = QueueEncryption.KMS,
                EncryptionMasterKey = EncryptionKey,
                Fifo = props.Fifo ? props.Fifo : null,
                ContentBasedDeduplication = props.Fifo ? props.Fifo : null,
                EnforceSSL = true,
                RetentionPeriod = Duration.Days(props.RetentionPeriodDays ?? 4),
                VisibilityTimeout = Duration.Seconds(props.VisibilityTimeoutSeconds ?? 30)
            }
        );
    }
}
