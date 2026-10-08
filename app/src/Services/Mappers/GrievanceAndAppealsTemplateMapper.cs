using Models.Event;
using ServiceClients.Navitus;

namespace Services.Mappers;

public static class GrievanceAndAppealsTemplateMapper
{
    /// <summary>
    /// Maps Episode data to a CreateEpisodeNoteRequest
    /// Note will appear to be entered by the user/system which created the Episode.
    /// </summary>
    /// <param name="episode"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static CreateEpisodeNoteRequest MapToTemplatedGrievanceAndAppealsEpisodeNoteRequest(this GrievanceAndAppealsEpisodeCreated episode)
    {
        var result = new CreateEpisodeNoteRequest()
        {
            NavitusEpisodeId = episode.NavitusEpisodeId,
            EnteredBy = episode.EnteredBy ?? throw new Exception("EnteredBy is null when it should not be."),
            SubjectLine = MapGrievanceAndAppealsSubjectLine(episode.GrievanceAndAppealsStatus),
            Text = MapGrievanceAndAppealsText(episode)
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
    public static CreateEpisodeNoteRequest MapToTemplatedGrievanceAndAppealsEpisodeNoteRequest(this EpisodeBase episode)
    {
        var result = new CreateEpisodeNoteRequest()
        {
            NavitusEpisodeId = episode.NavitusEpisodeId,
            EnteredBy = episode.ModifiedBy ?? throw new Exception("ModifiedBy is null when it should not be."),
            SubjectLine = MapGrievanceAndAppealsSubjectLine(episode.GrievanceAndAppealsStatus),
            Text = MapGrievanceAndAppealsText(episode)
        };

        return result;
    }

    /// <summary>
    /// Maps the Subject Line for the Call Tracking Note
    /// </summary>
    /// <param name="gnaStatus"></param>
    /// <returns></returns>
    private static string MapGrievanceAndAppealsSubjectLine(GrievanceAndAppealsStatus gnaStatus)
    {
        var formattedStatus = gnaStatus.ToString() switch
        {
            nameof(GrievanceAndAppealsStatus.Dismissed) => "Dismissed",
            nameof(GrievanceAndAppealsStatus.NA) => "NonAppealable",
            nameof(GrievanceAndAppealsStatus.Excluded) => "Excluded",
            nameof(GrievanceAndAppealsStatus.ReopenApproved) => "Reopen-Approved",
            nameof(GrievanceAndAppealsStatus.ReopenDenied) => "Reopen-Denied",
            nameof(GrievanceAndAppealsStatus.PartiallyFavorable) => "Partially Favorable",
            nameof(GrievanceAndAppealsStatus.FullyFavorable) => "Fully Favorable",
            nameof(GrievanceAndAppealsStatus.Postponed) => "Postponed",
            nameof(GrievanceAndAppealsStatus.Withdrawn) => "Withdrawn",
            nameof(GrievanceAndAppealsStatus.Preapproval) => "Pre-approval",
            nameof(GrievanceAndAppealsStatus.Approved) => "Approved",
            nameof(GrievanceAndAppealsStatus.Denied) => "Denied",
            nameof(GrievanceAndAppealsStatus.ReceivedInError) => "Received In Error",
            nameof(GrievanceAndAppealsStatus.Abandoned) => "Abandoned",
            nameof(GrievanceAndAppealsStatus.Resolved) => "Resolved",
            nameof(GrievanceAndAppealsStatus.Unresolved) => "Unresolved",
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
    public static string MapGrievanceAndAppealsText(EpisodeBase updatedEpisode)
    {
        var noteSegments = new List<(string, string?)>();
        string result = string.Empty;
        switch (updatedEpisode.GrievanceAndAppealsStatus.ToString())
        {
            case nameof(GrievanceAndAppealsStatus.Dismissed):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Dismissed");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.NA):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "NonAppealable");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Excluded):
            {
                var segments = (
                    new[]
                    {
                        ("Denial Reason", updatedEpisode.DenialReason),
                        ("RPh Reviewer", updatedEpisode.RPhReviewerId),
                        ("Decision Notes", updatedEpisode.DecisionLanguage)
                    }
                )
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Excluded");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.ReopenApproved):
            {
                var segments = (
                    new[]
                    {
                        ("Duration", updatedEpisode.ApprovalDuration),
                        ("MSP", updatedEpisode.MandatorySpecialtyPharmacy),
                        ("Tier", updatedEpisode.CoverageLevel),
                        ("Quantity Limit", updatedEpisode.QuantityLimit)
                    }
                )
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Reopen-Approved");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.ReopenDenied):
            {
                var segments = (
                    new[]
                    {
                        ("Denial Reason", updatedEpisode.DenialReason),
                        ("RPh Reviewer", updatedEpisode.RPhReviewerId),
                        ("Decision Notes", updatedEpisode.DecisionLanguage)
                    }
                )
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Reopen-Denied");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.PartiallyFavorable):
            {
                var segments = (
                    new[]
                    {
                        ("Denial Reason", updatedEpisode.DenialReason),
                        ("RPh Reviewer", updatedEpisode.RPhReviewerId),
                        ("Decision Notes", updatedEpisode.DecisionLanguage)
                    }
                )
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Partially Favorable");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.FullyFavorable):
            {
                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Fully Favorable");
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Postponed):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Postponed");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Withdrawn):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Withdrawn");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Preapproval):
            {
                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Pre-approval");
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Approved):
            {
                var segments = (
                    new[]
                    {
                        ("Duration", updatedEpisode.ApprovalDuration),
                        ("MSP", updatedEpisode.MandatorySpecialtyPharmacy),
                        ("Tier", updatedEpisode.CoverageLevel),
                        ("Quantity Limit", updatedEpisode.QuantityLimit)
                    }
                )
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Approved");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Denied):
            {
                var segments = (
                    new[]
                    {
                        ("Denial Reason", updatedEpisode.DenialReason),
                        ("RPh Reviewer", updatedEpisode.RPhReviewerId),
                        ("Decision Notes", updatedEpisode.DecisionLanguage)
                    }
                )
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Denied");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.ReceivedInError):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Received In Error");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Abandoned):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Abandoned");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Resolved):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Resolved");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            case nameof(GrievanceAndAppealsStatus.Unresolved):
            {
                var segments = (new[] { ("Reason", updatedEpisode.Reason), ("Description", updatedEpisode.DecisionLanguage) })
                    .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                    .ToList();
                noteSegments.AddRange(segments);

                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "Unresolved");
                result += MapGrievanceAndAppealsTextSegments(noteSegments);
                break;
            }
            default:
            {
                result = MapGenericGrievanceAndAppealsTextSegments(updatedEpisode, "In Progress");
                break;
            }
        }
        ;
        return result;
    }

    /// <summary>
    /// Map Generic segment of call tracking note that all statuses will use
    /// </summary>
    /// <param name="episode"></param>
    /// <param name="outcome"></param>
    /// <returns></returns>

    private static string MapGenericGrievanceAndAppealsTextSegments(EpisodeBase episode, string outcome)
    {
        var grievanceCaseTypes = new string[]
        {
            nameof(GrievanceAndAppealsCaseType.Grievance),
            nameof(GrievanceAndAppealsCaseType.FormalComplaint),
            nameof(GrievanceAndAppealsCaseType.Inquiry),
            nameof(GrievanceAndAppealsCaseType.MedDGriev),
            nameof(GrievanceAndAppealsCaseType.BBBComplaint),
            nameof(GrievanceAndAppealsCaseType.OCIComplaint),
            nameof(GrievanceAndAppealsCaseType.ComplaintAssist),
            nameof(GrievanceAndAppealsCaseType.CTM),
            nameof(GrievanceAndAppealsCaseType.TexasMedicaidProviderComplaint),
            nameof(GrievanceAndAppealsCaseType.URACComplaint)
        };
        var isGrievanceCaseType = grievanceCaseTypes.Contains(episode.GrievanceAndAppealsCaseType.ToString());

        var segments = isGrievanceCaseType
            ? (
                new[]
                {
                    ("Type", episode.GrievanceAndAppealsCaseType.ToString()),
                    ("Drug", episode.DrugDescription),
                    ("Outcome", outcome),
                    ("Banjo Case ID", episode.ExternalEpisodeId),
                    ("Due", episode.DueDateTime.ToNCRxDateTime()),
                    ("Finalized", episode.FinalizationDateTime.ToNCRxDateTime())
                }
            )
                .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                .ToList()
            : (
                new[]
                {
                    ("Type", episode.GrievanceAndAppealsCaseType.ToString()),
                    ("Banjo Related PA Case ID", episode.ExternalRelatedPriorAuthEpisodeId),
                    ("Drug", episode.DrugDescription),
                    ("Outcome", outcome),
                    ("Banjo Case ID", episode.ExternalEpisodeId),
                    ("Due", episode.DueDateTime.ToNCRxDateTime()),
                    ("Finalized", episode.FinalizationDateTime.ToNCRxDateTime()),
                    ("Appeal Type", MapGrievanceAndAppealsType(episode))
                }
            )
                .Where(s => !string.IsNullOrWhiteSpace(s.Item2))
                .ToList();

        var noteSegments = new List<(string type, string? value)>();
        noteSegments.AddRange(segments);

        var noteText = string.Join(
            $"\r\n",
            noteSegments.Select(segment => segment.type + ": " + (string.IsNullOrWhiteSpace(segment.value) ? "NOT_APPLICABLE" : segment.value))
        );

        return noteText;
    }

    /// <summary>
    /// Map segment of call trakcing note specific to status
    /// </summary>
    /// <param name="noteSegments"></param>
    /// <returns></returns>
    private static string MapGrievanceAndAppealsTextSegments(List<(string type, string? value)> noteSegments)
    {
        if (noteSegments.Count > 0)
        {
            var noteText =
                $"\r\n"
                + string.Join(
                    $"\r\n",
                    noteSegments.Select(segment =>
                        segment.type + ": " + (string.IsNullOrWhiteSpace(segment.value) ? "NOT_APPLICABLE" : segment.value)
                    )
                );

            return noteText;
        }
        return string.Empty;
    }

    /// <summary>
    /// Map Appeal Type segment
    /// </summary>
    /// <param name="episode"></param>
    /// <returns></returns>
    private static string MapGrievanceAndAppealsType(EpisodeBase episode)
    {
        if (episode.AppealReason == GrievanceAndAppealsReason.Test)
        {
            return "Test";
        }

        if (episode.AppealReason == GrievanceAndAppealsReason.Reopen)
        {
            return "Reopen";
        }

        if (episode.CarrierId == "NVMTU")
        {
            if (episode.AppealLevel == 2 && episode.AppealReason == GrievanceAndAppealsReason.Appeal)
            {
                return "Appeal";
            }

            if (episode.AppealLevel == 3 && episode.AppealReason == GrievanceAndAppealsReason.IRO)
            {
                return "IRO";
            }
        }

        switch (episode.LineOfBusiness)
        {
            case LineOfBusiness.MedicareD:
                return GetMedicareDAppealType(episode);
            case LineOfBusiness.Commercial:
                return GetCommercialAppealType(episode);
            case LineOfBusiness.Medicaid:
                return GetMedicaidAppealType(episode);
        }

        return string.Empty;
    }

    private static string GetMedicareDAppealType(EpisodeBase episode)
    {
        if (
            episode.AppealLevel == 1
            && (episode.AppealReason == GrievanceAndAppealsReason.RD || episode.AppealReason == GrievanceAndAppealsReason.LEP)
        )
        {
            return "Redetermination";
        }

        if (
            episode.AppealLevel == 2
            && (episode.AppealReason == GrievanceAndAppealsReason.IRE || episode.AppealReason == GrievanceAndAppealsReason.IREAutoForward)
        )
        {
            return "IRE";
        }

        if (episode.AppealLevel == 2 && episode.AppealReason == GrievanceAndAppealsReason.ManualClaimsRD)
        {
            return "Internal Appeal";
        }

        if (episode.AppealLevel == 3 && episode.AppealReason == GrievanceAndAppealsReason.ALJ)
        {
            return "ALJ";
        }

        if (episode.AppealLevel == 4 && episode.AppealReason == GrievanceAndAppealsReason.MAC)
        {
            return "MAC";
        }

        return string.Empty;
    }

    private static string GetCommercialAppealType(EpisodeBase episode)
    {
        if (
            episode.AppealLevel == 1
            && (
                episode.AppealReason == GrievanceAndAppealsReason.Appeal
                || episode.AppealReason == GrievanceAndAppealsReason.DAW
                || episode.AppealReason == GrievanceAndAppealsReason.ReReview
            )
        )
        {
            return "Internal Appeal";
        }

        if (episode.AppealLevel == 2 && episode.AppealReason == GrievanceAndAppealsReason.IRO)
        {
            return "IRO";
        }

        return string.Empty;
    }

    private static string GetMedicaidAppealType(EpisodeBase episode)
    {
        if (episode.AppealLevel == 1 && episode.AppealReason == GrievanceAndAppealsReason.NorthCarolinaMedicaid)
        {
            return "Internal Appeal";
        }

        if (episode.AppealLevel == 2 && episode.AppealReason == GrievanceAndAppealsReason.StateFairHearing)
        {
            return "State Hearing";
        }

        return string.Empty;
    }
}
