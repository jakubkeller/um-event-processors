using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Models.Event;
using Services.Interface;

namespace EmailProcessor;

public class EmailSqsRecordHandler(IEmailService _emailService) : ISqsRecordHandler
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing outbound email message: {record.MessageId}");
        var @event =
            JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(record.Body, _jsonSerializerOptions)
            ?? throw new Exception("Invalid Outbound Email Received Event");
        Logger.LogInformation($"outboundEmailReceivedEvent.DetailType {@event.DetailType}");

        if (@event.DetailType == nameof(OutboundEmailReceived))
        {
            var outboundEmailReceivedEvent =
                @event.Detail.GetProperty("payload").Deserialize<OutboundEmailReceived>(_jsonSerializerOptions)
                ?? throw new Exception("Unable to deserialize event into OutboundEmailReceivedEvent");
            Logger.LogInformation($"Processing OutboundEmailReceivedEvent for EpisodeNumber {outboundEmailReceivedEvent.NavitusEpisodeId}");
            await _emailService.ProcessOutboundEmailReceivedEvent(outboundEmailReceivedEvent);
        }
        else
        {
            throw new NotImplementedException($"Event detail-type {@event.DetailType} not implemented");
        }

        return RecordHandlerResult.None;
    }
}
