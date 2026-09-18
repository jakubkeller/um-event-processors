using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus;
using Services.Interface;
using Services.Mappers;
using Shared.Authorization;
using Shared.Services.Auth;
using Microsoft.Graph.Models;

namespace Services.Implementation;

public class SendEmailService : ISendEmailService
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;
    private readonly IGraphEmailClient _graphEmailClient;
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;
    private const string NOTE_SUBJECT = "Outreach Email Sent";

    public SendEmailService(
        IAuthProvider authProvider,
        IUMServiceClient umServiceClient,
        IFileService fileService,
        IGraphEmailClient graphEmailClient,
        IConfiguration configuration)
    {
        _authProvider = authProvider;
        _umServiceClient = umServiceClient;
        _umServiceClient.BaseUrl =
            configuration.GetValue<string>("NavitusUtilizationManagementBaseURL")
            ?? throw new Exception("Missing required configuration value NavitusUtilizationManagementBaseURL");
        _fileService = fileService;
        _graphEmailClient = graphEmailClient;
        _configuration = configuration;    
    }

    /// <summary>
    /// Ensures that the function has a Cognito OAuth Token ready in case UM API needs to be invoked.
    /// </summary>
    public async Task EnsureAuthenticated()
    {
        Logger.LogInformation("--- SendEmailService.EnsureAuthenticated Start ---");

        string[] requiredScopes = [Scopes.Client.NucleusNavitus, Scopes.PriorAuthEpisode.Read, Scopes.PriorAuthEpisode.Write];

        _umServiceClient.BearerToken = await _authProvider.AuthenticateInternallyNucleus(requiredScopes);

        Logger.LogDebug("--- SendEmailService.EnsureAuthenticated Complete ---");
    }

    /// <summary>
    /// Process Send Email Event.
    /// </summary>
    /// <param name="SendEmailEvent">SendEmailEvent</param>
    /// <returns>boolean indicating whether process completed successfully.</returns>
    public async Task<bool> ProcessSendEmailEvent(SendEmail @event)
    {
        var fromAddress = _configuration.GetValue<string>("GRAPH_CLIENT_FROM_ADDRESS")
            ?? throw new Exception("Missing required configuration value GRAPH_CLIENT_FROM_ADDRESS");

        var graphMessage = @event.MapToGraphMessage(fromAddress);

        foreach (var documentId in @event.DocumentId)
        {

            var presignedUrlRequest = new GeneratePresignedURLEmailRequest()
            {
                DocumentId = documentId
            };

            var presignedUrlResponse = await _umServiceClient.Episode_GeneratePresignedURLAsync(presignedUrlRequest);

            if (presignedUrlResponse is null || presignedUrlResponse.StatusCode != 200)
            {
                Logger.LogError($"Unable to retrieve presigned URL for DocumentId: {documentId}");
                continue;
            }

            try
            {
                var fileData = await _fileService.DownloadFileAsBytesAsync(presignedUrlResponse.Result.PresignedUrl);
                if (graphMessage.Attachments is null)
                {
                    graphMessage.Attachments = new List<Attachment>();
                }

                graphMessage.Attachments.Add(new FileAttachment
                {
                    Name = documentId,
                    ContentType = "application/pdf",
                    ContentBytes = fileData
                });

            }
            catch (Exception ex)
            {
                Logger.LogError($"Unable to download file from S3 for DocumentId: {documentId}");
                Logger.LogError(ex);
            }
        }

        Logger.LogInformation($"--- Processing Send Email for NavitusEpisodeId: {@event.NavitusEpisodeId} ---");

        await _graphEmailClient.SendEmailAsync(graphMessage, true);

        var newEmailNote = new CreateEpisodeNoteRequest()
        {
            NavitusEpisodeId = long.Parse(@event.NavitusEpisodeId),
            EnteredBy = "UMEmailService",
            Text = MapEmailMetadataToNoteBody(@event),
            SubjectLine = NOTE_SUBJECT
        };

        await _umServiceClient.CreateEpisodeNoteAsync(newEmailNote);
        return true;
    }

    private string MapEmailMetadataToNoteBody(SendEmail sendEmail)
    {
        var centralDate = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
        
        var sb = new System.Text.StringBuilder();

        sb.Append("Outreach email sent at ").Append(centralDate.ToString("M/d/yyyy h:mm:ss tt") + " CT").Append('.')
        .AppendLine();

        sb.Append("To: ")
        .AppendLine(string.Join("; ", sendEmail.ToEmailaddress ?? Enumerable.Empty<string>()));

        sb.Append("CC: ")
        .AppendLine(string.Join("; ", sendEmail.CcEmailaddress ?? Enumerable.Empty<string>()));

        sb.Append("BCC: ")
        .AppendLine(string.Join("; ", sendEmail.BccEmailaddress ?? Enumerable.Empty<string>()));

        sb.Append("Subject: ")
        .Append(sendEmail.Subject);

        return sb.ToString();
    }
}
