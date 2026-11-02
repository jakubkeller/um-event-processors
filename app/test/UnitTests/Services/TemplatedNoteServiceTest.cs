using FluentAssertions;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using Services.Implementation;
using Xunit;

namespace UnitTests.Services
{
    public class TemplatedNoteServiceTest : BaseTests
    {
        private readonly TemplatedNoteService _templatedNoteService;

        public TemplatedNoteServiceTest()
        {
            _templatedNoteService = new TemplatedNoteService(mockAuthProvider, mockUMServiceClient, configuration);
        }

        [Fact]
        public async Task ProcessEventUpdatedHandler_TemplatedNote_Is_Needed()
        {
            //Arrange
            var oldEpisodeNoteRequest = GetEpisodeBase();
            var newEpisodeNoteRequest = GetEpisodeBase();
            newEpisodeNoteRequest.ApprovalDuration = "1 Year";
            newEpisodeNoteRequest.PriorAuthStatus = PriorAuthStatus.Approved;
            newEpisodeNoteRequest.Reason = "Test-Reason Updated";
            newEpisodeNoteRequest.DenialReason = "Test-DenialReason Updated";

            var createEpisodeUpdatedV2 = new EpisodeUpdatedV2() { OldEpisode = oldEpisodeNoteRequest, NewEpisode = newEpisodeNoteRequest };

            //Act
            var noteNeeded = await _templatedNoteService.ProcessEpisodeUpdatedV2Event(createEpisodeUpdatedV2);

            //Assert
            await mockUMServiceClient.Received(1).CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>());
            noteNeeded.Should().Be(true);
        }

        [Fact]
        public async Task ProcessEventUpdatedHandler_TemplatedNote_Is_Not_Needed()
        {
            //Arrange
            var oldEpisodeNoteRequest = GetEpisodeBase();
            var newEpisodeNoteRequest = GetEpisodeBase();

            var createEpisodeUpdatedV2 = new EpisodeUpdatedV2() { OldEpisode = oldEpisodeNoteRequest, NewEpisode = newEpisodeNoteRequest };

            //Act
            var noteNeeded = await _templatedNoteService.ProcessEpisodeUpdatedV2Event(createEpisodeUpdatedV2);

            //Assert
            await mockUMServiceClient.DidNotReceive().CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>());
            noteNeeded.Should().Be(false);
        }

        [Fact]
        public async Task ProcessGrievanceAndAppealEventUpdatedHandler_TemplatedNote_Is_Needed()
        {
            //Arrange
            var oldEpisodeNoteRequest = GetGrievanceAndAppealEpisodeBase();
            var newEpisodeNoteRequest = GetGrievanceAndAppealEpisodeBase();
            newEpisodeNoteRequest.ApprovalDuration = "1 Year";
            newEpisodeNoteRequest.Reason = "Test-Reason Updated";
            newEpisodeNoteRequest.DenialReason = "Test-DenialReason Updated";
            newEpisodeNoteRequest.GrievanceAndAppealsStatus = GrievanceAndAppealsStatus.Withdrawn;

            var createGrievanceAndAppealsEpisodeUpdated = new GrievanceAndAppealsEpisodeUpdated()
            {
                OldEpisode = oldEpisodeNoteRequest,
                NewEpisode = newEpisodeNoteRequest
            };

            //Act
            var noteNeeded = await _templatedNoteService.ProcessGrievanceAndAppealsEpisodeUpdatedEvent(createGrievanceAndAppealsEpisodeUpdated);

            //Assert
            await mockUMServiceClient.Received(1).CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>());
            noteNeeded.Should().Be(true);
        }

        [Fact]
        public async Task ProcessGrievanceAndAppealEventUpdatedHandler_TemplatedNote_Is_Not_Needed()
        {
            //Arrange
            var oldEpisodeNoteRequest = GetGrievanceAndAppealEpisodeBase();
            var newEpisodeNoteRequest = GetGrievanceAndAppealEpisodeBase();

            var createGrievanceAndAppealsEpisodeUpdated = new GrievanceAndAppealsEpisodeUpdated()
            {
                OldEpisode = oldEpisodeNoteRequest,
                NewEpisode = newEpisodeNoteRequest
            };

            //Act
            var noteNeeded = await _templatedNoteService.ProcessGrievanceAndAppealsEpisodeUpdatedEvent(createGrievanceAndAppealsEpisodeUpdated);

            //Assert
            await mockUMServiceClient.DidNotReceive().CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>());
            noteNeeded.Should().Be(false);
        }
    }
}
