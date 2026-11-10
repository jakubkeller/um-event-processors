using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Models.Event;
using Services.Interface;

namespace RtpbReceiveProcessor
{
    public class RtpbReceiveSqsRecordHandler(IRtpbReportingStorageService _rtpbReportingStorageService) : ISqsRecordHandler
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Processing record message: {record.MessageId}");

            var @event =
                JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(record.Body, _jsonSerializerOptions)
                ?? throw new Exception("Invalid RTPB Receive Event");

            Logger.LogInformation($"rtpbEvent.DetailType {@event.DetailType}");

            switch (@event.DetailType)
            {
                case nameof(RtpbReceiveEvent):
                    var rtpbReceiveEvent =
                        @event.Detail.Deserialize<RtpbReceiveEvent>(_jsonSerializerOptions)
                        ?? throw new Exception("Unable to deserialize event into RtpbReceiveEvent");

                    Logger.LogInformation($"Processing RtpbReceiveEvent for RequestMessageId {rtpbReceiveEvent.RequestMessageId}");

                    await _rtpbReportingStorageService.StoreRtpbTransactionAsync(rtpbReceiveEvent);
                    break;
                default:
                    throw new NotImplementedException($"Event detail-type {@event.DetailType} not implemented");
            }

            return RecordHandlerResult.None;
        }
    }
}
