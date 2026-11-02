using Microsoft.Extensions.Configuration;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using Services.Implementation;
using Services.Interface;
using Shared.Authorization;
using Shared.Services.Auth;
using Xunit;
using Microsoft.Graph.Models;

namespace UnitTests.Services
{
    public class SendEmailServiceTests
    {
        private readonly IAuthProvider _authProvider;
        private readonly IUMServiceClient _umServiceClient;
        private readonly IConfiguration _configuration;
        private readonly IFileService _fileService;
        private readonly IGraphEmailClient _graphEmailClient;
        private readonly SendEmailService _sendEmailService;

        public SendEmailServiceTests()
        {
            _authProvider = Substitute.For<IAuthProvider>();
            _umServiceClient = Substitute.For<IUMServiceClient>();
            _configuration = Substitute.For<IConfiguration>();
            _fileService = Substitute.For<IFileService>();
            _graphEmailClient = Substitute.For<IGraphEmailClient>();

            _configuration.GetValue<string>("NavitusUtilizationManagementBaseURL").Returns("http://test-url.com");

            _sendEmailService = new SendEmailService(
                _authProvider,
                _umServiceClient,
                _fileService,
                _graphEmailClient,
                _configuration
            );
        }

        [Fact]
        public async Task EnsureAuthenticated_ShouldSetBearerToken()
        {
            // Arrange
            string expectedToken = "test-token";
            _authProvider.AuthenticateInternallyNucleus(Arg.Any<string[]>()).Returns(expectedToken);

            // Act
            await _sendEmailService.EnsureAuthenticated();

            // Assert
            await _authProvider
                .Received(1)
                .AuthenticateInternallyNucleus(
                    Arg.Is<string[]>(scopes => scopes[0] == Scopes.Client.NucleusNavitus && scopes[1] == Scopes.PriorAuthEpisode.Read)
                );
            Assert.Equal(expectedToken, _umServiceClient.BearerToken);
        }

        [Fact]
        public async Task ProcessSendEmailEventNoAttachments_ShouldCallProcessSendEmailEvent()
        {
            // Arrange
            var sendEmail = new SendEmail
            {
                NavitusEpisodeId = "123",
                Subject = "Subject",
                HtmlBody = "Body",
                Sensitivity = 0,
                ToEmailaddress = ["person@place.com"],
                CcEmailaddress = ["person@place.com"],
                BccEmailaddress = [],
                DocumentId = []
            };

            // Act
            var result = await _sendEmailService.ProcessSendEmailEvent(sendEmail);

            // Assert
            await _graphEmailClient.Received(1).SendEmailAsync(Arg.Any<Message>(), Arg.Any<bool>());
            await _umServiceClient.Received(1).CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>());
            Assert.True(result);
        }

        [Fact]
        public async Task ProcessSendEmailEventWithAttachments_ShouldCallProcessSendEmailEvent()
        {
            // Arrange
            var sendEmail = new SendEmail
            {
                NavitusEpisodeId = "123",
                Subject = "Subject",
                HtmlBody = "Body",
                Sensitivity = 0,
                ToEmailaddress = ["person@place.com"],
                CcEmailaddress = ["person@place.com"],
                BccEmailaddress = [],
                DocumentId = ["123", "456"]
            };


            var presignedUrlResponse = new ApiResponse<GeneratePresignedURLEmailResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GeneratePresignedURLEmailResponse
                {
                    DocumentId = "test-url"
                }
            );

            _umServiceClient
                .Episode_GeneratePresignedURLAsync(Arg.Any<GeneratePresignedURLEmailRequest>())
                .Returns(presignedUrlResponse);

            _fileService
                .DownloadFileAsBytesAsync(Arg.Any<string>())
                .Returns(new byte[] { 1, 2, 3 });

            // Act
            var result = await _sendEmailService.ProcessSendEmailEvent(sendEmail);

            // Assert
            await _umServiceClient.Received(2).Episode_GeneratePresignedURLAsync(Arg.Any<GeneratePresignedURLEmailRequest>());
            await _fileService.Received(2).DownloadFileAsBytesAsync(Arg.Any<string>());
            await _graphEmailClient.Received(1).SendEmailAsync(Arg.Any<Message>(), Arg.Any<bool>());
            await _umServiceClient.Received(1).CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>());
            Assert.True(result);
        }
    }
}
