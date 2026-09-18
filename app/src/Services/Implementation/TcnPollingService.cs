using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus.IntegrationPriorAuth;
using ServiceClients.Navitus;
using Services.Interface;
using Shared.Authorization;
using Shared.Services.Auth;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Implementation;

public class TcnPollingService : ITcnPollingService
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;
    private readonly IIntegrationPriorAuthServiceClient _integrationPriorAuthServiceClient;
    private readonly IConfiguration _configuration;
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly string _eventBusName;
    private readonly string _eventSource;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

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
        _eventBridgeClient = new AmazonEventBridgeClient();
        _eventBusName = configuration.GetValue<string>("UMEventBusArn")
            ?? throw new Exception("Missing required configuration value UMEventBusArn");
        _eventSource = configuration.GetValue<string>("UMEventSource") ?? "com.navitus.um";
        _jsonSerializerOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
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
                var reportResponse = await _integrationPriorAuthServiceClient.GetFtpReportAsync(detail.TcnTaskSid, cancellationToken);
                var callResult = reportResponse.Result?.CallResults?.FirstOrDefault();

                await _umServiceClient.UpdateCallDetailAsync(new UpdateTcnCallDetailRequest
                {
                    TcnTaskSid = detail.TcnTaskSid,
                    CallResult = callResult?.ShortResult,
                    CompletedAt = callResult?.LastAttemptedAt,
                    DurationSeconds = callResult?.DurationSeconds,
                }, cancellationToken);

                Logger.LogInformation($"--- Updated TCN call detail for TaskSid: {detail.TcnTaskSid}, Result: {callResult?.ShortResult} ---");

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

        // Send batched failure notification email if there were any failures
        if (failures.Count > 0)
        {
            await SendBatchFailureNotificationAsync(failures, cancellationToken);
        }
    }

    private async Task SendBatchFailureNotificationAsync(List<(long EpisodeNumber, string TaskSid, FtpCallResult CallResult)> failures, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"--- Sending batch failure notification for {failures.Count} failed TCN calls ---");

        var paTeamDistributionList = _configuration.GetValue<string>("TcnFailureNotificationDistributionList")
            ?? throw new Exception("Missing required configuration: TcnFailureNotificationDistributionList");

        var toAddresses = paTeamDistributionList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var outboundEmailReceivedEvent = new OutboundEmailReceived
        {
            NavitusEpisodeId = failures[0].EpisodeNumber, // Use first episode as primary
            Subject = "Unsuccessful Oral Notification(s)",
            HtmlBody = BuildBatchFailureEmailBody(failures),
            Sensitivity = 0,
            ToEmailaddress = toAddresses,
            CcEmailaddress = Array.Empty<string>(),
            BccEmailaddress = Array.Empty<string>(),
            DocumentId = Enumerable.Empty<string>()
        };

        await _eventBridgeClient.PutEventsAsync(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    Source = _eventSource,
                    EventBusName = _eventBusName,
                    DetailType = nameof(OutboundEmailReceived),
                    Detail = JsonSerializer.Serialize(outboundEmailReceivedEvent, _jsonSerializerOptions)
                }
            }
        }, cancellationToken);

        Logger.LogInformation($"--- TCN Batch Failure Notification Published for {failures.Count} episodes ---");
    }

    private static string BuildBatchFailureEmailBody(List<(long EpisodeNumber, string TaskSid, FtpCallResult CallResult)> failures)
    {
        var rows = new System.Text.StringBuilder();

        foreach (var (episodeNumber, _, callResult) in failures)
        {
            var completedAt = callResult.LastAttemptedAt?.ToLocalTime().ToString("M/d/yyyy h:mm:ss tt") ?? "N/A";
            rows.AppendLine($"            <tr>");
            rows.AppendLine($"                <td>{callResult.BanjoCaseId ?? "N/A"}</td>");
            rows.AppendLine($"                <td>{episodeNumber}</td>");
            rows.AppendLine($"                <td>{callResult.MemberFirstName} {callResult.MemberLastName}</td>");
            rows.AppendLine($"                <td>{completedAt}</td>");
            rows.AppendLine($"            </tr>");
        }

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
        <p>The following case(s) had unsuccessful oral notifications:</p>

        <table>
            <tr>
                <th>Banjo Case ID</th>
                <th>Call Tracking ID</th>
                <th>Member Name</th>
                <th>Phone call attempt date/time</th>
            </tr>
{rows}
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
