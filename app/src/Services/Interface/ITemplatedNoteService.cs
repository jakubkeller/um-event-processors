using Models.Event;

namespace Services.Interface
{
    public interface ITemplatedNoteService
    {
        Task<bool> ProcessEpisodeCreatedEvent(EpisodeCreated episodeCreated);
        Task<bool> ProcessEpisodeUpdatedV2Event(EpisodeUpdatedV2 episodeUpdatedV2Event);
        Task<bool> ProcessGrievanceAndAppealsEpisodeCreatedEvent(GrievanceAndAppealsEpisodeCreated grievanceAndAppealsEpisodeCreatedEvent);
        Task<bool> ProcessGrievanceAndAppealsEpisodeUpdatedEvent(GrievanceAndAppealsEpisodeUpdated grievanceAndAppealsEpisodeUpdatedEvent);
        Task EnsureAuthenticated();
    }
}
