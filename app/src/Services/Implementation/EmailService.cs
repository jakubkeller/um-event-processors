using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus;
using Services.Interface;
using Services.Mappers;
using Shared.Authorization;
using Shared.Services.Auth;

namespace Services.Implementation;

public class EmailService : IEmailService
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;

    public EmailService(IAuthProvider authProvider, IUMServiceClient umServiceClient, IConfiguration configuration)
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
        Logger.LogInformation("--- EmailService.EnsureAuthenticated Start ---");

        string[] requiredScopes = [Scopes.Client.NucleusNavitus, Scopes.PriorAuthEpisode.Write];

        _umServiceClient.BearerToken = await _authProvider.AuthenticateInternallyNucleus(requiredScopes);

        Logger.LogDebug("--- EmailService.EnsureAuthenticated Complete ---");
    }

    /// <summary>
    /// Process Outbound Email Received Event and store the email metadata by inovking the UM API.
    /// </summary>
    /// <param name="OutboundEmailReceivedEvent">OutboundEmailReceivedEvent</param>
    /// <returns>boolean indicating whether process completed successfully.</returns>
    public async Task<bool> ProcessOutboundEmailReceivedEvent(OutboundEmailReceived @event)
    {
        var request = EmailMapper.MapToCreateEmailMetadataRequest(@event);

        Logger.LogInformation($"--- Processing Email Metadata for Outbound Email for NavitusEpisodeId: {@event.NavitusEpisodeId} ---");

        await _umServiceClient.Episode_CreateEmailMetadataAsync(request);

        return true;
    }
}
