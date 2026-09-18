using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;

namespace DocumentMetadataProcessor
{
    public class SqsBatchProcessorWithLogging : SqsBatchProcessor
    {
        protected override Task HandleRecordFailureAsync(SQSEvent.SQSMessage record, Exception exception)
        {
            Logger.LogError($"--- Record Failed to process: {record?.MessageId} ---");
            Logger.LogError(exception);

            return base.HandleRecordFailureAsync(record, exception);
        }
    }
}
