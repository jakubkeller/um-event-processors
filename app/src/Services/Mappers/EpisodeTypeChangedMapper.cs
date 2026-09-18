using Models.Event;
using ServiceClients.Navitus;

namespace Services.Mappers;

public static class EpisodeTypeChangedMapper
{
    /// <summary>
    /// Maps Updated Episode data to a CreateEpisodeNoteRequest when episode type has changed
    /// Note will appear to be entered by the user who committed the update to the Episode.
    /// </summary>
    /// <param name="episode"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static CreateEpisodeNoteRequest MapToTemplatedEpisodeTypeChangedNoteRequest(
        this EpisodeBase episode,
        string toEpisodeType,
        string? modifiedBy
    )
    {
        var result = new CreateEpisodeNoteRequest()
        {
            NavitusEpisodeId = episode.NavitusEpisodeId,
            EnteredBy = modifiedBy ?? throw new Exception("ModifiedBy is null when it should not be."),
            SubjectLine = "Episode type updated to " + toEpisodeType,
            Text = MapTextSegments(episode, toEpisodeType)
        };

        return result;
    }

    /// <summary>
    /// Map Generic segment of call tracking note that all episode type changes will use
    /// </summary>
    /// <param name="episode"></param>
    /// <param name="outcome"></param>
    /// <returns></returns>

    private static string MapTextSegments(EpisodeBase episode, string toEpisodeType)
    {
        string? noteText;
        var segments = new List<(string, string?)>();
        if (toEpisodeType == "PA_EXT")
        {
            segments = (
                new[]
                {
                    ("GrievanceAndAppealsStatus", episode.GrievanceAndAppealsStatus.ToString()),
                    ("GrievanceAndAppealsCaseType", episode.GrievanceAndAppealsCaseType.ToString()),
                    ("RelatedPriorAuthEpisodeNumber", episode.NavitusRelatedPriorAuthEpisodeId.ToString()),
                    ("ExternalRelatedPriorAuthEpisodeId", episode.ExternalRelatedPriorAuthEpisodeId)
                }
            )
                .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
                .ToList();
        }
        else if (toEpisodeType == "GNA_EXT")
        {
            segments = [.. (new[] { ("PriorAuthStatus", episode.PriorAuthStatus.ToString()) }).Where(x => !string.IsNullOrWhiteSpace(x.Item2))];
        }
        var noteSegments = new List<(string type, string? value)>();
        noteSegments.AddRange(segments);

        noteText =
            "Previous Episode Values"
            + $"\r\n"
            + string.Join(
                $"\r\n",
                noteSegments.Select(segment => segment.type + ": " + (string.IsNullOrWhiteSpace(segment.value) ? "NOT_APPLICABLE" : segment.value))
            );

        return noteText;
    }
}
