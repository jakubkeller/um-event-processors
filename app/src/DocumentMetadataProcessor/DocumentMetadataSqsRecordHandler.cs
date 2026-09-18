using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Models.Event;
using Services.Interface;

namespace DocumentMetadataProcessor
{
    public class DocumentMetadataSqsRecordHandler(IDocumentService _documentService) : ISqsRecordHandler
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Processing record message: {record.MessageId}");

            var @event =
                JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(record.Body, _jsonSerializerOptions)
                ?? throw new Exception("Invalid Document Received Event");

            Logger.LogInformation($"outboundEvent.DetailType {@event.DetailType}");

            switch (@event.DetailType)
            {
                case nameof(OutboundMailReceived):
                    var outboundMailReceivedEvent =
                        @event.Detail.GetProperty("payload").Deserialize<OutboundMailReceived>(_jsonSerializerOptions)
                        ?? throw new Exception("Unable to deserialize event into OutboundMailReceivedEvent");

                    Logger.LogInformation($"Processing OutboundMailReceivedEvent for EpisodeNumber {outboundMailReceivedEvent.NavitusEpisodeId}");

                    await _documentService.ProcessOutboundMailReceivedEvent(outboundMailReceivedEvent);
                    break;
                case nameof(OutboundFaxReceived):
                    var outboundFaxReceivedEvent =
                        @event.Detail.GetProperty("payload").Deserialize<OutboundFaxReceived>(_jsonSerializerOptions)
                        ?? throw new Exception("Unable to deserialize event into OutboundFaxReceivedEvent");

                    Logger.LogInformation($"Processing OutboundFaxReceivedEvent for EpisodeNumber {outboundFaxReceivedEvent.NavitusEpisodeId}");

                    await _documentService.ProcessOutboundFaxReceivedEvent(outboundFaxReceivedEvent);
                    break;
                default:
                    throw new NotImplementedException($"Event detail-type {@event.DetailType} not implemented");
            }
            return RecordHandlerResult.None;
        }
    }
}
