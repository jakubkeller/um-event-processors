using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus;
using Services.Interface;
using Services.Mappers;
using Shared.Authorization;
using Shared.Services.Auth;

namespace Services.Implementation
{
    public class TemplatedNoteService : ITemplatedNoteService
    {
        private readonly IAuthProvider _authProvider;
        private readonly IUMServiceClient _umServiceClient;

        public TemplatedNoteService(IAuthProvider authProvider, IUMServiceClient umServiceClient, IConfiguration configuration)
        {
            _authProvider = authProvider;
            _umServiceClient = umServiceClient;
            _umServiceClient.BaseUrl =
                configuration.GetValue<string>("NavitusUtilizationManagementBaseURL")
                ?? throw new Exception("Missing required configuration value NavitusUtilizationManagementBaseURL");
        }

        /// <summary>
        /// Ensures that the function has a Cognito OAuth Token ready in case UM API needs to be invoked.
        /// </summary>
        public async Task EnsureAuthenticated()
        {
            Logger.LogInformation("--- TemplatedNoteService.EnsureAuthenticated Start ---");

            string[] requiredScopes = [Scopes.Client.NucleusNavitus, Scopes.PriorAuthEpisode.Write];

            _umServiceClient.BearerToken = await _authProvider.AuthenticateInternallyNucleus(requiredScopes);

            Logger.LogDebug("--- TemplatedNoteService.EnsureAuthenticated Complete ---");
        }

        /// <summary>
        /// Given an episode created event we determine if a note needs to be created.
        /// If a note needs to be created, this function determines the appropriate note text and creates the note by calling UM API.
        /// </summary>
        /// <param name="EpisodeCreatedEvent">EpisodeCreated</param>
        /// <returns>boolean indicating whether a note is processed.</returns>
        public async Task<bool> ProcessEpisodeCreatedEvent(EpisodeCreated @event)
        {
            var newCreateEpisodeNoteRequest = TemplateMapper.MapToTemplatedEpisodeNoteRequest(@event);

            Logger.LogInformation($"--- Templated Note Required for NavitusEpisodeId: {@event.NavitusEpisodeId} ---");

            await _umServiceClient.CreateEpisodeNoteAsync(newCreateEpisodeNoteRequest);

            return true;
        }

        /// <summary>
        /// Given an episode updated event (v2) we determine if a note needs to be created.
        /// If a note needs to be created, this function determines the appropriate note text and creates the note by calling UM API.
        /// </summary>
        /// <param name="episodeUpdatedV2Event">EpisodeUpdatedV2</param>
        /// <returns>boolean indicating whether a note is processed.</returns>
        public async Task<bool> ProcessEpisodeUpdatedV2Event(EpisodeUpdatedV2 @event)
        {
            if (@event.OldEpisode.GrievanceAndAppealsStatus != 0)
            {
                Logger.LogInformation($"--- Episode Type Changed Note Required for NavitusEpisodeId: {@event.OldEpisode.NavitusEpisodeId} ---");

                var newEpisodeTypeChangedNote = EpisodeTypeChangedMapper.MapToTemplatedEpisodeTypeChangedNoteRequest(
                    @event.OldEpisode,
                    "PA_EXT",
                    @event.NewEpisode.ModifiedBy
                );

                await _umServiceClient.CreateEpisodeNoteAsync(newEpisodeTypeChangedNote);
            }

            var oldText = TemplateMapper.MapText(@event.OldEpisode);

            var newCreateEpisodeNoteRequest = TemplateMapper.MapToTemplatedEpisodeNoteRequest(@event.NewEpisode);

            if (TemplateNoteNeeded(oldText, newCreateEpisodeNoteRequest.Text))
            {
                Logger.LogInformation($"--- Templated Note Required for NavitusEpisodeId: {@event.OldEpisode.NavitusEpisodeId} ---");

                await _umServiceClient.CreateEpisodeNoteAsync(newCreateEpisodeNoteRequest);

                return true;
            }

            Logger.LogInformation($"--- Templated Note Not Required for NavitusEpisodeId: {@event.OldEpisode.NavitusEpisodeId} ---");

            return false;
        }

        /// <summary>
        /// Logic to determine if a new note needs to be created.
        /// </summary>
        /// <param name="oldEpisodeNoteText">Generated Note text for the original episode data.</param>
        /// <param name="newEpisodeNoteText">Generated Note text for the updated episode data.</param>
        /// <returns></returns>
        private static bool TemplateNoteNeeded(string oldEpisodeNoteText, string newEpisodeNoteText)
        {
            return oldEpisodeNoteText != newEpisodeNoteText;
        }

        /// <summary>
        /// Given a grievance and appeals episode created event we determine if a note needs to be created.
        /// If a note needs to be created, this function determines the appropriate note text and creates the note by calling UM API.
        /// </summary>
        /// <param name="GrievanceAndAppealsEpisodeCreatedEvent">GrievanceAndAppealsEpisodeCreated</param>
        /// <returns>boolean indicating whether a note is processed.</returns>
        public async Task<bool> ProcessGrievanceAndAppealsEpisodeCreatedEvent(GrievanceAndAppealsEpisodeCreated @event)
        {
            var newCreateEpisodeNoteRequest = GrievanceAndAppealsTemplateMapper.MapToTemplatedGrievanceAndAppealsEpisodeNoteRequest(@event);

            Logger.LogInformation($"--- Templated Note Required for NavitusEpisodeId: {@event.NavitusEpisodeId} ---");

            await _umServiceClient.CreateEpisodeNoteAsync(newCreateEpisodeNoteRequest);

            return true;
        }

        /// <summary>
        /// Given a grievance and appeals episode updated event we determine if a note needs to be created.
        /// If a note needs to be created, this function determines the appropriate note text and creates the note by calling UM API.
        /// </summary>
        /// <param name="GrievanceAndAppealsEpisodeUpdatedEvent">GrievanceAndAppealsEpisodeUpdated</param>
        /// <returns>boolean indicating whether a note is processed.</returns>
        public async Task<bool> ProcessGrievanceAndAppealsEpisodeUpdatedEvent(GrievanceAndAppealsEpisodeUpdated @event)
        {
            if (@event.OldEpisode.PriorAuthStatus != 0)
            {
                Logger.LogInformation($"--- Episode Type Changed Note Required for NavitusEpisodeId: {@event.OldEpisode.NavitusEpisodeId} ---");

                var newEpisodeTypeChangedNote = EpisodeTypeChangedMapper.MapToTemplatedEpisodeTypeChangedNoteRequest(
                    @event.OldEpisode,
                    "GNA_EXT",
                    @event.NewEpisode.ModifiedBy
                );

                await _umServiceClient.CreateEpisodeNoteAsync(newEpisodeTypeChangedNote);
            }

            var oldText = GrievanceAndAppealsTemplateMapper.MapGrievanceAndAppealsText(@event.OldEpisode);

            var newCreateEpisodeNoteRequest = GrievanceAndAppealsTemplateMapper.MapToTemplatedGrievanceAndAppealsEpisodeNoteRequest(
                @event.NewEpisode
            );

            if (TemplateNoteNeeded(oldText, newCreateEpisodeNoteRequest.Text))
            {
                Logger.LogInformation($"--- Templated Note Required for NavitusEpisodeId: {@event.OldEpisode.NavitusEpisodeId} ---");

                await _umServiceClient.CreateEpisodeNoteAsync(newCreateEpisodeNoteRequest);

                return true;
            }

            Logger.LogInformation($"--- Templated Note Not Required for NavitusEpisodeId: {@event.OldEpisode.NavitusEpisodeId} ---");

            return false;
        }
    }
}
