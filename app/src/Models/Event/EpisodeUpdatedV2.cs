namespace Models.Event
{
    public class EpisodeUpdatedV2
    {
        public EpisodeBase OldEpisode { get; set; } = default!;
        public EpisodeBase NewEpisode { get; set; } = default!;
    }
}
