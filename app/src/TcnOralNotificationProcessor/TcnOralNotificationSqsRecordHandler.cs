using System.Text.Json;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Models.Event;
using Services.Interface;

namespace TcnOralNotificationProcessor;

public class TcnOralNotificationSqsRecordHandler(ITcnOralNotificationService _tcnOralNotificationService) : ISqsRecordHandler
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing record message: {record.MessageId}");

        var eventBridgeEvent =
            JsonSerializer.Deserialize<CloudWatchEvent<TcnInformationEvent>>(record.Body, _jsonSerializerOptions)
            ?? throw new Exception("Invalid TcnInformationEvent event");

        var @event = eventBridgeEvent.Detail ?? throw new Exception("TcnInformationEvent detail is null");

        Logger.LogInformation($"Processing TcnInformationEvent for EpisodeId: {@event.EpisodeNumber}, Source: {@event.EpisodeSourceType}, Status: {@event.PriorAuthStatus}");

        await _tcnOralNotificationService.ScheduleBroadcastAsync(@event, cancellationToken);

        return RecordHandlerResult.None;
    }
}
