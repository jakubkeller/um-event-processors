namespace Models.Event;

public class GrievanceAndAppealsEpisodeUpdated
{
    public EpisodeBase OldEpisode { get; set; } = default!;
    public EpisodeBase NewEpisode { get; set; } = default!;
}
