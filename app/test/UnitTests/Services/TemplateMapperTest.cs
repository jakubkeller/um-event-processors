using FluentAssertions;
using ServiceClients.Navitus;
using Services.Mappers;
using Xunit;

namespace UnitTests.Services
{
    public class TemplateMapperTest : BaseTests
    {
        [Theory]
        [InlineData(
            PriorAuthStatus.InProgress,
            "In Progress in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: In Progress\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.Approved,
            "Approved in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: Approved\r\nBanjo Case ID: Transaction-123\r\nDuration: 8 months\r\nMSP: Test-MandatorySpecialtyPharmacy\r\nTier: Test-CoverageLevel\r\nQuantity Limit: Test-QuantityLimit\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.Denied,
            "Denial in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: Denied\r\nBanjo Case ID: Transaction-123\r\nDenial Reason: Test-DenialReason\r\nRPh Reviewer: Test-RPhReviewerId\r\nDecision Notes: Test-DecisionLanguage\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.Abandoned,
            "Cancelled in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: Cancelled\r\nBanjo Case ID: Transaction-123\r\nReason: Test-Reason\r\nDescription: Test-DecisionLanguage\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.PartiallyApproved,
            "Partial Approval in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: Partially Approved\r\nBanjo Case ID: Transaction-123\r\nDenial Reason: Test-DenialReason\r\nRPh Reviewer: Test-RPhReviewerId\r\nDecision Notes: Test-DecisionLanguage\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.FollowupNeeded,
            "In Progress in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: In Progress\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.FormFaxedToProvider,
            "In Progress in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: In Progress\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        [InlineData(
            PriorAuthStatus.EPAAwaitingAnswers,
            "ePA Awaiting Answers in Banjo",
            "Drug: Test-DrugDescription\r\nOutcome: ePA Awaiting Answers\r\nBanjo Case ID: Transaction-123\r\nFinalized: 02/01/2000 18:00:00 CT"
        )]
        public void MapToTemplatedEpisodeNoteRequest_GivenAnEpisode_ShouldCorrectlyMapSubjectLine(
            PriorAuthStatus priorAuthStatus,
            string expectedSubjectLine,
            string expectedText
        )
        {
            //Arange
            var episodeTemplate = GetEpisodeBase();
            episodeTemplate.PriorAuthStatus = priorAuthStatus;

            //Act
            var result = TemplateMapper.MapToTemplatedEpisodeNoteRequest(episodeTemplate);

            //Assert
            result.SubjectLine.Should().Be(expectedSubjectLine);
            result.Text.Should().Be(expectedText);
        }
    }
}
