using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Models.Event;
using Services.Interface;

namespace SendEmailProcessor;

public class SendEmailSqsRecordHandler(ISendEmailService _sendEmailService) : ISqsRecordHandler
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Processing record message: {record.MessageId}");

        var @event =
            JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(record.Body, _jsonSerializerOptions)
            ?? throw new Exception("Invalid Send Email Event");

        Logger.LogInformation($"sendEmailEvent.DetailType {@event.DetailType}");

        if (@event.DetailType == nameof(SendEmail))
        {
            var sendEmailEvent =
                @event.Detail.Deserialize<SendEmail>(_jsonSerializerOptions)
                ?? throw new Exception("Unable to deserialize event into SendEmailEvent");

            Logger.LogInformation($"Processing SendEmailEvent for EpisodeNumber {sendEmailEvent.NavitusEpisodeId}");

            await _sendEmailService.ProcessSendEmailEvent(sendEmailEvent);
        }
        else
        {
            throw new NotImplementedException($"Event detail-type {@event.DetailType} not implemented");
        }
        return RecordHandlerResult.None;
    }
}
