using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus.IntegrationPriorAuth;
using ServiceClients.Navitus;
using Services.Interface;
using Services.Mappers;
using Shared.Authorization;
using Shared.Services.Auth;

namespace Services.Implementation;

public class TcnPollingService : ITcnPollingService
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;
    private readonly IIntegrationPriorAuthServiceClient _integrationPriorAuthServiceClient;
    private readonly IConfiguration _configuration;

    public TcnPollingService(
        IAuthProvider authProvider,
        IUMServiceClient umServiceClient,
        IIntegrationPriorAuthServiceClient integrationPriorAuthServiceClient,
        IConfiguration configuration)
    {
        _authProvider = authProvider;
        _umServiceClient = umServiceClient;
        _umServiceClient.BaseUrl =
            configuration.GetValue<string>("NavitusUtilizationManagementBaseURL")
            ?? throw new Exception("Missing required configuration value NavitusUtilizationManagementBaseURL");
        _integrationPriorAuthServiceClient = integrationPriorAuthServiceClient;
        _integrationPriorAuthServiceClient.BaseUrl =
            configuration.GetValue<string>("IntegrationPriorAuthBaseURL")
            ?? throw new Exception("Missing required configuration value IntegrationPriorAuthBaseURL");
        _configuration = configuration;
    }

    public async Task EnsureAuthenticated()
    {
        Logger.LogInformation("--- TcnPollingService.EnsureAuthenticated Start ---");

        string[] requiredScopes = [Scopes.Client.NucleusNavitus, Scopes.PriorAuthEpisode.Read, Scopes.PriorAuthEpisode.Write];

        var token = await _authProvider.AuthenticateInternallyNucleus(requiredScopes);
        _umServiceClient.BearerToken = token;
        _integrationPriorAuthServiceClient.BearerToken = token;

        Logger.LogDebug("--- TcnPollingService.EnsureAuthenticated Complete ---");
    }

    public async Task PollPendingCallDetailsAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pendingResponse = await _umServiceClient.GetPendingCallDetailsAsync(batchSize, cancellationToken);
        var pending = pendingResponse.Result?.PendingCallDetails;

        if (pending == null || pending.Count == 0)
        {
            Logger.LogInformation("--- No pending TCN call details to poll. ---");
            return;
        }

        Logger.LogInformation($"--- Polling {pending.Count} pending TCN call details. ---");

        var failures = new List<(long EpisodeNumber, string TaskSid, FtpCallResult CallResult)>();

        foreach (var detail in pending)
        {
            try
            {
                // The report is keyed by campaign TaskSid and can contain results for every contact
                // dropped into that campaign, not just this one - match this detail's own episode.
                var reportResponse = await _integrationPriorAuthServiceClient.GetFtpReportAsync(detail.TcnTaskSid, cancellationToken);
                var callResult = reportResponse.Result?.CallResults?
                    .FirstOrDefault(r => r.EpisodeNumber == detail.EpisodeNumber.ToString());

                await _umServiceClient.UpdateCallDetailAsync(new UpdateTcnCallDetailRequest
                {
                    TcnTaskSid = detail.TcnTaskSid,
                    EpisodeNumber = detail.EpisodeNumber,
                    CallResult = callResult?.ShortResult,
                    CompletedAt = callResult?.LastAttemptedAt,
                    DurationSeconds = callResult?.DurationSeconds,
                }, cancellationToken);

                Logger.LogInformation(
                    "--- Updated TCN call detail for EpisodeNumber: {EpisodeNumber}, Result: {CallResult} ---",
                    detail.EpisodeNumber,
                    callResult?.ShortResult
                );

                if (callResult != null)
                {
                    var noteText = BuildCallNoteText(detail.EpisodeNumber, callResult);
                    var isFailure = !string.IsNullOrEmpty(callResult.ShortResult) && IsFailureResult(callResult.ShortResult);

                    await _umServiceClient.CreateEpisodeNoteAsync(new ServiceClients.Navitus.CreateEpisodeNoteRequest
                    {
                        NavitusEpisodeId = detail.EpisodeNumber,
                        EnteredBy = "TCNCallTracking",
                        Text = noteText,
                        SubjectLine = isFailure ? "TCN Call Failure" : "TCN Call Attempt"
                    }, cancellationToken);

                    if (isFailure)
                    {
                        failures.Add((detail.EpisodeNumber, detail.TcnTaskSid, callResult));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"--- Failed to poll TCN call detail for TaskSid: {detail.TcnTaskSid} ---");
            }
        }

        // Send one failure notification email per failed episode, since each email
        // gets attached to that episode's documents and must not reference other episodes.
        if (failures.Count > 0)
        {
            await SendFailureNotificationsAsync(failures, cancellationToken);
        }
    }

    private async Task SendFailureNotificationsAsync(List<(long EpisodeNumber, string TaskSid, FtpCallResult CallResult)> failures, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"--- Sending failure notifications for {failures.Count} failed TCN calls ---");

        var paTeamDistributionList = _configuration.GetValue<string>("TcnFailureNotificationDistributionList")
            ?? throw new Exception("Missing required configuration: TcnFailureNotificationDistributionList");

        var toAddresses = paTeamDistributionList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var failure in failures)
        {
            try
            {
                var outboundEmailReceivedEvent = new OutboundEmailReceived
                {
                    NavitusEpisodeId = failure.EpisodeNumber,
                    Subject = "Unsuccessful Oral Notification",
                    HtmlBody = BuildFailureEmailBody(failure.EpisodeNumber, failure.CallResult),
                    Sensitivity = 0,
                    ToEmailaddress = toAddresses,
                    CcEmailaddress = Array.Empty<string>(),
                    BccEmailaddress = Array.Empty<string>(),
                    DocumentId = Enumerable.Empty<string>()
                };

                var request = outboundEmailReceivedEvent.MapToCreateEmailMetadataRequest();
                await _umServiceClient.Episode_CreateEmailMetadataAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"--- Failed to send failure notification for EpisodeNumber: {failure.EpisodeNumber} ---");
            }
        }

        Logger.LogInformation($"--- TCN Failure Notifications Sent for {failures.Count} episodes ---");
    }

    private static string BuildFailureEmailBody(long episodeNumber, FtpCallResult callResult)
    {
        var completedAt = callResult.LastAttemptedAt?.ToLocalTime().ToString("M/d/yyyy h:mm:ss tt") ?? "N/A";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .content {{ padding: 20px; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class='content'>
        <p>The following case had an unsuccessful oral notification:</p>

        <table>
            <tr>
                <th>Banjo Case ID</th>
                <th>Call Tracking ID</th>
                <th>Member Name</th>
                <th>Phone call attempt date/time</th>
            </tr>
            <tr>
                <td>{callResult.BanjoCaseId ?? "N/A"}</td>
                <td>{episodeNumber}</td>
                <td>{callResult.MemberFirstName} {callResult.MemberLastName}</td>
                <td>{completedAt}</td>
            </tr>
        </table>
    </div>
</body>
</html>";
    }

    private static bool IsFailureResult(string result)
    {
        return result switch
        {
            "Busy" => true,
            "No Answer" => true,
            "Invalid" => true,
            "Failed" => true,
            "Machine Hangup" => true,
            "Machine Undeliverable" => true,
            "Fax" => true,
            _ => false
        };
    }

    private static string BuildCallNoteText(long episodeNumber, FtpCallResult callResult)
    {
        var completedAt = callResult.LastAttemptedAt?.ToLocalTime().ToString("M/d/yyyy h:mm:ss tt") ?? "N/A";
        var result = callResult.ShortResult ?? "Pending";

        var sb = new System.Text.StringBuilder();

        sb.Append("TCN oral notification call ").Append(result).Append(" at ").Append(completedAt).Append('.')
          .AppendLine();

        sb.Append("Member: ").Append(callResult.MemberFirstName).Append(' ').Append(callResult.MemberLastName).AppendLine()
          .Append("Member ID: ").Append(callResult.MemberId ?? "N/A").AppendLine()
          .Append("Result: ").Append(result);

        if (IsFailureResult(result))
        {
            sb.AppendLine().Append("FAILURE - PA team notified.");
        }

        return sb.ToString();
    }
}
