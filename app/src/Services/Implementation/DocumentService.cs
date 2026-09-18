using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus;
using ServiceClients.Navitus.Document;
using Services.Interface;
using Services.Mappers;
using Shared.Authorization;
using Shared.Services.Auth;

namespace Services.Implementation
{
    public class DocumentService : IDocumentService
    {
        private readonly IAuthProvider _authProvider;
        private readonly IUMDocumentServiceClient _umDocumentServiceClient;

        public DocumentService(IAuthProvider authProvider, IUMDocumentServiceClient umDocumentServiceClient, IConfiguration configuration)
        {
            _authProvider = authProvider;
            _umDocumentServiceClient = umDocumentServiceClient;
            _umDocumentServiceClient.BaseUrl =
                configuration.GetValue<string>("NavitusUtilizationManagementBaseURL")
                ?? throw new Exception("Missing required configuration value NavitusUtilizationManagementBaseURL");
        }

        /// <summary>
        /// Ensures that the function has a Cognito OAuth Token ready in case UM API needs to be invoked.
        /// </summary>
        public async Task EnsureAuthenticated()
        {
            Logger.LogInformation("--- DocumentService.EnsureAuthenticated Start ---");

            string[] requiredScopes = [Scopes.Client.NucleusNavitus, Scopes.PriorAuthDocument.Write];

            _umDocumentServiceClient.BearerToken = await _authProvider.AuthenticateInternallyNucleus(requiredScopes);

            Logger.LogDebug("--- DocumentService.EnsureAuthenticated Complete ---");
        }

        /// <summary>
        /// Process Outbound Mail Received Event and store the document metadata by inovking the UM API.
        /// </summary>
        /// <param name="OutboundMailReceivedEvent">OutboundMailReceivedEvent</param>
        /// <returns>boolean indicating whether process completed successfully.</returns>
        public async Task<bool> ProcessOutboundMailReceivedEvent(OutboundMailReceived @event)
        {
            var request = DocumentMetadataMapper.MapToCreateDocumentMetadataRequest(@event);

            Logger.LogInformation(
                $"--- Processing Document Metadata for Mail for NavitusEpisodeId: {@event.NavitusEpisodeId}, TransactionId: {@event.TransactionId}, {@event.S3ObjectKey} ---"
            );

            await _umDocumentServiceClient.Document_CreateDocumentMetadataAsync(request);

            return true;
        }

        /// <summary>
        /// Process Outbound Fax Received Event and store the document metadata by inovking the UM API.
        /// </summary>
        /// <param name="OutboundFaxReceivedEvent">OutboundMailReceivedEvent</param>
        /// <returns>boolean indicating whether process completed successfully.</returns>
        public async Task<bool> ProcessOutboundFaxReceivedEvent(OutboundFaxReceived @event)
        {
            var request = DocumentMetadataMapper.MapToCreateDocumentMetadataRequest(@event);

            Logger.LogInformation(
                $"--- Processing Document Metadata for Fax for NavitusEpisodeId: {@event.NavitusEpisodeId}, TransactionId: {@event.TransactionId}, {@event.S3ObjectKey} ---"
            );

            await _umDocumentServiceClient.Document_CreateDocumentMetadataAsync(request);

            return true;
        }
    }
}
