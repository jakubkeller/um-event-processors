using FluentAssertions;
using ServiceClients.Navitus;
using Services.Mappers;
using Xunit;

namespace UnitTests.Services;

public class GrievanceAndAppealsTemplateMapperTest : BaseTests
{
    [Theory]
    [InlineData(
        GrievanceAndAppealsStatus.Dismissed,
        "Dismissed in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Dismissed\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.NA,
        "NonAppealable in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: NonAppealable\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Excluded,
        "Excluded in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Excluded\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDenial Reason: Test-DenialReason\r\nRPh Reviewer: Test-RPhReviewerId\r\nDecision Notes: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.ReopenApproved,
        "Reopen-Approved in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Reopen-Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.ReopenDenied,
        "Reopen-Denied in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Reopen-Denied\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDenial Reason: Test-DenialReason\r\nRPh Reviewer: Test-RPhReviewerId\r\nDecision Notes: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.PartiallyFavorable,
        "Partially Favorable in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Partially Favorable\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDenial Reason: Test-DenialReason\r\nRPh Reviewer: Test-RPhReviewerId\r\nDecision Notes: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.FullyFavorable,
        "Fully Favorable in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Fully Favorable\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Postponed,
        "Postponed in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Postponed\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Withdrawn,
        "Withdrawn in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Withdrawn\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Preapproval,
        "Pre-approval in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Pre-approval\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Approved,
        "Approved in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Denied,
        "Denied in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Denied\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDenial Reason: Test-DenialReason\r\nRPh Reviewer: Test-RPhReviewerId\r\nDecision Notes: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.ReceivedInError,
        "Received In Error in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Received In Error\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Abandoned,
        "Cancelled in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Cancelled\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.InProgress,
        "In Progress in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: In Progress\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Resolved,
        "Resolved in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Resolved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    [InlineData(
        GrievanceAndAppealsStatus.Unresolved,
        "Unresolved in Banjo",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Unresolved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage"
    )]
    public void MapToTemplatedEpisodeNoteRequest_GivenAGrievanceAndAppealsEpisode_ShouldCorrectlyMapSubjectLine(
        GrievanceAndAppealsStatus grievanceAndAppealsStatus,
        string expectedSubjectLine,
        string expectedText
    )
    {
        //Arange
        var episodeTemplate = GetGrievanceAndAppealEpisodeBase();
        episodeTemplate.GrievanceAndAppealsStatus = grievanceAndAppealsStatus;

        //Act
        var result = GrievanceAndAppealsTemplateMapper.MapToTemplatedGrievanceAndAppealsEpisodeNoteRequest(episodeTemplate);

        //Assert
        result.SubjectLine.Should().Be(expectedSubjectLine);
        result.Text.Should().Be(expectedText);
    }

    [Theory]
    [InlineData(
        3,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.ALJ,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: ALJ\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.Appeal,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Internal Appeal\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.Appeal,
        "NVMTU",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Appeal\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.DAW,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Internal Appeal\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.IRE,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: IRE\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.IREAutoForward,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: IRE\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.IRO,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: IRO\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        3,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.IRO,
        "NVMTU",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: IRO\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.LEP,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Redetermination\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        4,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.MAC,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: MAC\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.ManualClaimsRD,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Internal Appeal\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.Medicaid,
        GrievanceAndAppealsReason.NorthCarolinaMedicaid,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Internal Appeal\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.MedicareD,
        GrievanceAndAppealsReason.RD,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Redetermination\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.ReReview,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Internal Appeal\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.Commercial,
        GrievanceAndAppealsReason.Reopen,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Reopen\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.Medicaid,
        GrievanceAndAppealsReason.StateFairHearing,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: State Hearing\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        2,
        LineOfBusiness.Medicaid,
        GrievanceAndAppealsReason.Test,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nAppeal Type: Test\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    [InlineData(
        1,
        LineOfBusiness.Medicaid,
        GrievanceAndAppealsReason.RD,
        "",
        "Type: Appeal\r\nBanjo Related PA Case ID: RelatedTran-123\r\nDrug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit"
    )]
    public void MapToTemplatedEpisodeNoteRequest_GivenAGrievanceAndAppealsEpisode_ShouldCorrectlyMapAppealType(
        int appealLevel,
        LineOfBusiness lineOfBusiness,
        GrievanceAndAppealsReason appealReason,
        string carrierId,
        string expectedText
    )
    {
        //Arrange
        var episodeTemplate = GetGrievanceAndAppealEpisodeBase();
        episodeTemplate.AppealLevel = appealLevel;
        episodeTemplate.LineOfBusiness = lineOfBusiness;
        episodeTemplate.AppealReason = appealReason;
        episodeTemplate.CarrierId = carrierId;

        //Act
        var result = GrievanceAndAppealsTemplateMapper.MapToTemplatedGrievanceAndAppealsEpisodeNoteRequest(episodeTemplate);

        //Assert
        result.Text.Should().Be(expectedText);
    }
}
