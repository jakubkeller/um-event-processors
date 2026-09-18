using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Models.Event;
using Services.Interface;

namespace TemplatedNoteProcessor
{
    public class TemplatedNoteProcessorSqsRecordHandler(ITemplatedNoteService _templatedNoteService) : ISqsRecordHandler
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        public async Task<RecordHandlerResult> HandleAsync(SQSEvent.SQSMessage record, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Processing record message: {record.MessageId}");

            var templatedNoteEvent =
                JsonSerializer.Deserialize<CloudWatchEvent<JsonElement>>(record.Body, _jsonSerializerOptions)
                ?? throw new Exception("Invalid Templated Note Event");

            Logger.LogInformation($"templatedNoteEvent.DetailType {templatedNoteEvent.DetailType}");

            switch (templatedNoteEvent.DetailType)
            {
                case nameof(EpisodeCreated):
                    var episodeCreated =
                        templatedNoteEvent.Detail.Deserialize<EpisodeCreated>(_jsonSerializerOptions)
                        ?? throw new Exception("Unable to deserialize event into EpisodeCreated");

                    Logger.LogInformation($"Processing templated note for EpisodeNumber {episodeCreated.NavitusEpisodeId}");

                    await _templatedNoteService.ProcessEpisodeCreatedEvent(episodeCreated);
                    break;
                case nameof(EpisodeUpdatedV2):
                    var episodeUpdatedV2 = templatedNoteEvent.Detail.Deserialize<EpisodeUpdatedV2>(_jsonSerializerOptions);

                    if (episodeUpdatedV2?.NewEpisode is null || episodeUpdatedV2?.OldEpisode is null)
                    {
                        throw new Exception("Unable to deserialize templatedNoteEvent.Detail as EpisodeUpdatedV2");
                    }
                    else
                    {
                        Logger.LogInformation($"Processing templated note for EpisodeNumber {episodeUpdatedV2.NewEpisode.NavitusEpisodeId}");

                        await _templatedNoteService.ProcessEpisodeUpdatedV2Event(episodeUpdatedV2);
                    }
                    break;
                case nameof(GrievanceAndAppealsEpisodeCreated):
                    var grievanceAndAppealsEpisodeCreated =
                        templatedNoteEvent.Detail.Deserialize<GrievanceAndAppealsEpisodeCreated>(_jsonSerializerOptions)
                        ?? throw new Exception("Unable to deserialize event into GrievanceAndAppealsEpisodeCreated");

                    Logger.LogInformation(
                        $"Processing templated note for Grienvance and Appeal EpisodeNumber {grievanceAndAppealsEpisodeCreated.NavitusEpisodeId}"
                    );

                    await _templatedNoteService.ProcessGrievanceAndAppealsEpisodeCreatedEvent(grievanceAndAppealsEpisodeCreated);
                    break;
                case nameof(GrievanceAndAppealsEpisodeUpdated):
                    var grievanceAndAppealsEpisodeUpdated = templatedNoteEvent.Detail.Deserialize<GrievanceAndAppealsEpisodeUpdated>(
                        _jsonSerializerOptions
                    );

                    if (grievanceAndAppealsEpisodeUpdated?.NewEpisode is null || grievanceAndAppealsEpisodeUpdated?.OldEpisode is null)
                    {
                        throw new Exception("Unable to deserialize templatedNoteEvent.Detail as GrievanceAndAppealsEpisodeUpdated");
                    }
                    else
                    {
                        Logger.LogInformation(
                            $"Processing templated note for Grievance and Appeal EpisodeNumber {grievanceAndAppealsEpisodeUpdated.NewEpisode.NavitusEpisodeId}"
                        );

                        await _templatedNoteService.ProcessGrievanceAndAppealsEpisodeUpdatedEvent(grievanceAndAppealsEpisodeUpdated);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Event detail-type {templatedNoteEvent.DetailType} not implemented");
            }
            return RecordHandlerResult.None;
        }
    }
}
