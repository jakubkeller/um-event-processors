using ServiceClients.Navitus;

namespace Models.Event;

public class EpisodeBase
{
    public long NavitusEpisodeId { get; set; }
    public MemberInformation MemberInformation { get; set; } = default!;
    public string? CarrierId { get; set; } = default!;
    public string? ExternalEpisodeId { get; set; }
    public EpisodeDrugIdentifiers DrugIdentifiers { get; set; } = default!;
    public string? DrugDescription { get; set; }
    public string? PrescriberId { get; set; } = default!;
    public EpisodeSourceType EpisodeSourceType { get; set; }
    public DateTimeOffset? FinalizationDateTime { get; set; }
    public string? DecisionLanguage { get; set; }
    public string? MandatorySpecialtyPharmacy { get; set; }
    public string? ApprovalDuration { get; set; }
    public string? RPhReviewerId { get; set; }
    public string? MDReviewerId { get; set; }
    public long? NavitusAuthorizationNumber { get; set; }
    public bool Urgent { get; set; }
    public PriorAuthStatus PriorAuthStatus { get; set; } = default!;
    public string EnteredBy { get; set; } = default!;
    public DateTimeOffset InitiationDateTime { get; set; }
    public string? CoverageLevel { get; set; }
    public string? QuantityLimit { get; set; }
    public string? Reason { get; set; }
    public string? DenialReason { get; set; }
    public DateTimeOffset? ModificationDateTime { get; set; }
    public string? ModifiedBy { get; set; }
    public string? ExternalRelatedPriorAuthEpisodeId { get; set; }
    public long? NavitusRelatedPriorAuthEpisodeId { get; set; }
    public GrievanceAndAppealsCaseType GrievanceAndAppealsCaseType { get; set; } = default!;
    public GrievanceAndAppealsStatus GrievanceAndAppealsStatus { get; set; } = default!;
    public DateTimeOffset? DueDateTime { get; set; }
    public int? AppealLevel { get; set; } = default!;
    public GrievanceAndAppealsReason? AppealReason { get; set; } = default!;
    public LineOfBusiness? LineOfBusiness { get; set; } = default!;
}
