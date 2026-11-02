using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using ServiceClients.Navitus.Document;
using Services.Implementation;
using Services.Interface;
using Shared.Authorization;
using Shared.Services.Auth;
using Xunit;

namespace UnitTests.Services
{
    public class DocumentServiceTests
    {
        private readonly IAuthProvider _authProvider;
        private readonly IUMDocumentServiceClient _umDocumentServiceClient;
        private readonly IConfiguration _configuration;
        private readonly DocumentService _documentService;

        public DocumentServiceTests()
        {
            _authProvider = Substitute.For<IAuthProvider>();
            _umDocumentServiceClient = Substitute.For<IUMDocumentServiceClient>();
            _configuration = Substitute.For<IConfiguration>();

            _configuration.GetValue<string>("NavitusUtilizationManagementBaseURL").Returns("http://test-url.com");

            _documentService = new DocumentService(_authProvider, _umDocumentServiceClient, _configuration);
        }

        [Fact]
        public async Task EnsureAuthenticated_ShouldSetBearerToken()
        {
            // Arrange
            string expectedToken = "test-token";
            _authProvider.AuthenticateInternallyNucleus(Arg.Any<string[]>()).Returns(expectedToken);

            // Act
            await _documentService.EnsureAuthenticated();

            // Assert
            await _authProvider
                .Received(1)
                .AuthenticateInternallyNucleus(
                    Arg.Is<string[]>(scopes => scopes[0] == Scopes.Client.NucleusNavitus && scopes[1] == Scopes.PriorAuthDocument.Write)
                );
            Assert.Equal(expectedToken, _umDocumentServiceClient.BearerToken);
        }

        [Fact]
        public async Task ProcessOutboundMailReceivedEvent_ShouldCallCreateDocumentMetadata()
        {
            // Arrange
            var outboundMailReceived = new OutboundMailReceived
            {
                NavitusEpisodeId = "123",
                TransactionId = "456",
                S3ObjectKey = "test-key"
            };

            // Act
            var result = await _documentService.ProcessOutboundMailReceivedEvent(outboundMailReceived);

            // Assert
            await _umDocumentServiceClient.Received(1).Document_CreateDocumentMetadataAsync(Arg.Any<CreateDocumentMetadataRequest>());
            Assert.True(result);
        }

        [Fact]
        public async Task ProcessOutboundFaxReceivedEvent_ShouldCallCreateDocumentMetadata()
        {
            // Arrange
            var outboundFaxReceived = new OutboundFaxReceived
            {
                NavitusEpisodeId = "789",
                TransactionId = "101",
                S3ObjectKey = "test-fax-key"
            };

            // Act
            var result = await _documentService.ProcessOutboundFaxReceivedEvent(outboundFaxReceived);

            // Assert
            await _umDocumentServiceClient.Received(1).Document_CreateDocumentMetadataAsync(Arg.Any<CreateDocumentMetadataRequest>());
            Assert.True(result);
        }
    }
}
