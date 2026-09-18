using System.Text;
using Models.Event;
using ServiceClients.Navitus;

namespace Services.Mappers
{
    public static class TemplateMapper
    {
        /// <summary>
        /// Maps Episode data to a CreateEpisodeNoteRequest
        /// Note will appear to be entered by the user/system which created the Episode.
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static CreateEpisodeNoteRequest MapToTemplatedEpisodeNoteRequest(this EpisodeCreated episode)
        {
            var result = new CreateEpisodeNoteRequest()
            {
                NavitusEpisodeId = episode.NavitusEpisodeId,
                EnteredBy = episode.EnteredBy ?? throw new Exception("EnteredBy is null when it should not be."),
                SubjectLine = MapSubjectLine(episode.PriorAuthStatus),
                Text = MapText(episode)
            };

            return result;
        }

        /// <summary>
        /// Maps Updated Episode data to a CreateEpisodeNoteRequest
        /// Note will appear to be entered by the user who committed the update to the Episode.
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static CreateEpisodeNoteRequest MapToTemplatedEpisodeNoteRequest(this EpisodeBase episode)
        {
            var result = new CreateEpisodeNoteRequest()
            {
                NavitusEpisodeId = episode.NavitusEpisodeId,
                EnteredBy = episode.ModifiedBy ?? throw new Exception("ModifiedBy is null when it should not be."),
                SubjectLine = MapSubjectLine(episode.PriorAuthStatus),
                Text = MapText(episode)
            };

            return result;
        }

        /// <summary>
        /// Maps the Subject Line for the Call Tracking Note
        /// </summary>
        /// <param name="priorAuthStatus"></param>
        /// <returns></returns>
        private static string MapSubjectLine(PriorAuthStatus priorAuthStatus)
        {
            var formattedStatus = priorAuthStatus.ToString() switch
            {
                nameof(PriorAuthStatus.Approved) => "Approved",
                nameof(PriorAuthStatus.Abandoned) => "Cancelled",
                nameof(PriorAuthStatus.Denied) => "Denial",
                nameof(PriorAuthStatus.PartiallyApproved) => "Partial Approval",
                nameof(PriorAuthStatus.EPAAwaitingAnswers) => "ePA Awaiting Answers",
                _ => "In Progress",
            };
            var result = $"{formattedStatus} in Banjo";

            return result;
        }

        /// <summary>
        /// Maps the Text of the Call Tracking Note
        /// Currently templated based mappings (based on the status of the latest Episode data)
        /// </summary>
        /// <param name="updatedEpisode"></param>
        /// <returns></returns>
        public static string MapText(EpisodeBase updatedEpisode)
        {
            var result = AppendIfNotEmpty("Drug", updatedEpisode.DrugDescription);

            switch (updatedEpisode.PriorAuthStatus.ToString())
            {
                case nameof(PriorAuthStatus.Approved):
                    result += AppendIfNotEmpty("Outcome", "Approved");
                    result += AppendIfNotEmpty("Banjo Case ID", updatedEpisode.ExternalEpisodeId);
                    result += AppendIfNotEmpty("Duration", updatedEpisode.ApprovalDuration);
                    result += AppendIfNotEmpty("MSP", updatedEpisode.MandatorySpecialtyPharmacy);
                    result += AppendIfNotEmpty("Tier", updatedEpisode.CoverageLevel);
                    result += AppendIfNotEmpty("Quantity Limit", updatedEpisode.QuantityLimit);
                    break;
                case nameof(PriorAuthStatus.Abandoned):
                    result += AppendIfNotEmpty("Outcome", "Cancelled");
                    result += AppendIfNotEmpty("Banjo Case ID", updatedEpisode.ExternalEpisodeId);
                    result += AppendIfNotEmpty("Reason", updatedEpisode.Reason);
                    result += AppendIfNotEmpty("Description", updatedEpisode.DecisionLanguage);
                    break;
                case nameof(PriorAuthStatus.Denied):
                    result += AppendIfNotEmpty("Outcome", "Denied");
                    result += AppendIfNotEmpty("Banjo Case ID", updatedEpisode.ExternalEpisodeId);
                    result += AppendIfNotEmpty("Denial Reason", updatedEpisode.DenialReason);
                    result += AppendIfNotEmpty("RPh Reviewer", updatedEpisode.RPhReviewerId);
                    result += AppendIfNotEmpty("Decision Notes", updatedEpisode.DecisionLanguage);
                    break;
                case nameof(PriorAuthStatus.PartiallyApproved):
                    result += AppendIfNotEmpty("Outcome", "Partially Approved");
                    result += AppendIfNotEmpty("Banjo Case ID", updatedEpisode.ExternalEpisodeId);
                    result += AppendIfNotEmpty("Denial Reason", updatedEpisode.DenialReason);
                    result += AppendIfNotEmpty("RPh Reviewer", updatedEpisode.RPhReviewerId);
                    result += AppendIfNotEmpty("Decision Notes", updatedEpisode.DecisionLanguage);
                    break;
               case nameof(PriorAuthStatus.EPAAwaitingAnswers):
                    result += AppendIfNotEmpty("Outcome", "ePA Awaiting Answers");
                    result += AppendIfNotEmpty("Banjo Case ID", updatedEpisode.ExternalEpisodeId);
                    break;
                default:
                    result += AppendIfNotEmpty("Outcome", "In Progress");
                    result += AppendIfNotEmpty("Banjo Case ID", updatedEpisode.ExternalEpisodeId);
                    break;
            }
            result += AppendIfNotEmpty("Due", updatedEpisode.DueDateTime.ToNCRxDateTime());
            result += AppendIfNotEmpty("Finalized", updatedEpisode.FinalizationDateTime.ToNCRxDateTime());
            return result.Trim();
        }

        private static string AppendIfNotEmpty(string label, string? value)
        {
            var response = string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
            {
                response = $"{label}: {value}\r\n".ToString();
            }
            return response;
        }
    }
}
