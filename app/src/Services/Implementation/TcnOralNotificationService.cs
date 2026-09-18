using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using ServiceClients.Navitus.IntegrationPriorAuth;
using ServiceClients.Navitus;
using ServiceClients.Navitus.MedicationHistory;
using Services.Interface;
using Shared.Authorization;
using Shared.Services.Auth;

namespace Services.Implementation;

public class TcnOralNotificationService : ITcnOralNotificationService
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;
    private readonly IIntegrationPriorAuthServiceClient _integrationPriorAuthServiceClient;
    private readonly IMedicationHistoryServiceClient? _medicationHistoryServiceClient;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> TcnEligibleStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        // PA statuses
        "Approved",
        "Denied",
        "PartiallyApproved",
        // G&A statuses
        "Dismissed",
        "Excluded",
        "ReopenApproved",
        "ReopenDenied",
        "PartiallyFavorable",
        "FullyFavorable",
        "Withdrawn"
    };

    private static readonly HashSet<string> CallerInfoPhoneSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "Phone",
        "Other",
        "ePA"
    };

    public TcnOralNotificationService(
        IAuthProvider authProvider,
        IUMServiceClient umServiceClient,
        IIntegrationPriorAuthServiceClient integrationPriorAuthServiceClient,
        IConfiguration configuration,
        IMedicationHistoryServiceClient? medicationHistoryServiceClient = null)
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
        _medicationHistoryServiceClient = medicationHistoryServiceClient;
        _configuration = configuration;
    }

    public async Task EnsureAuthenticated()
    {
        Logger.LogInformation("--- TcnOralNotificationService.EnsureAuthenticated Start ---");

        string[] requiredScopes = [Scopes.Client.NucleusNavitus, Scopes.PriorAuthEpisode.Write];

        var token = await _authProvider.AuthenticateInternallyNucleus(requiredScopes);
        _umServiceClient.BearerToken = token;
        _integrationPriorAuthServiceClient.BearerToken = token;

        Logger.LogDebug("--- TcnOralNotificationService.EnsureAuthenticated Complete ---");
    }

    public async Task ScheduleBroadcastAsync(TcnInformationEvent @event, CancellationToken cancellationToken)
    {
        if (!TcnEligibleStatuses.Contains(@event.PriorAuthStatus))
        {
            Logger.LogInformation($"--- TCN skipped for EpisodeId: {@event.EpisodeNumber}, Status: {@event.PriorAuthStatus} is not TCN-eligible ---");
            return;
        }

        var (homePhone, cellPhone, workPhone) = await ResolvePhoneNumbersAsync(@event, cancellationToken);

        if (string.IsNullOrWhiteSpace(homePhone) &&
            string.IsNullOrWhiteSpace(cellPhone) &&
            string.IsNullOrWhiteSpace(workPhone))
        {
            Logger.LogWarning($"--- No phone number resolved for EpisodeId: {@event.EpisodeNumber}, Source: {@event.EpisodeSourceType}, skipping TCN call ---");
            return;
        }

        var scheduleRequest = new ScheduleBroadcastRequest
        {
            ScheduleTemplate = _configuration.GetValue<int>("TCN_SCHEDULE_TEMPLATE"),
            ImportTemplate = _configuration.GetValue<int>("TCN_IMPORT_TEMPLATE"),
            Description = _configuration.GetValue<string>("TCN_DESCRIPTION") ?? throw new Exception("Missing required TCN_DESCRIPTION"),
            Timezone = _configuration.GetValue<string>("TCN_TIMEZONE") ?? throw new Exception("Missing required TCN_TIMEZONE"),
            DialOrder = _configuration.GetValue<string>("TCN_DIAL_ORDER") ?? throw new Exception("Missing required TCN_DIAL_ORDER"),
            BanjoCaseId = @event.ExternalEpisodeId,
            EpisodeNumber = @event.EpisodeNumber.ToString(),
            PaDetermination = @event.PriorAuthStatus,
            MemberFirstName = @event.MemberFirstName,
            MemberLastName = @event.MemberLastName,
            MemberId = @event.UniqueId,
            DateOfBirth = @event.DateOfBirth,
            Carrier = @event.CarrierId,
            Drug = @event.DrugDescription,
            MemberZipCode = @event.MemberZipCode,
            MemberHomePhone = homePhone ?? string.Empty,
            MemberWorkPhone = workPhone ?? string.Empty,
            MemberCellPhone = cellPhone ?? string.Empty,
            LanguageCode = @event.MemberLanguageCode,
        };

        var scheduleResponse = await _integrationPriorAuthServiceClient.ScheduleBroadcastAsync(scheduleRequest, cancellationToken);

        Logger.LogInformation($"--- Scheduled TCN broadcast for EpisodeId: {@event.EpisodeNumber}, TaskSid: {scheduleResponse.Result?.TaskSid} ---");

        await _umServiceClient.CreateCallDetailAsync(new CreateTcnCallDetailRequest
        {
            TcnTaskSid = scheduleResponse.Result?.TaskSid ?? throw new Exception("TCN schedule response missing TaskSid"),
            EpisodeNumber = @event.EpisodeNumber
        }, cancellationToken);

        Logger.LogInformation($"--- Created TCN call detail for EpisodeId: {@event.EpisodeNumber} ---");
    }

    /// <summary>
    /// Resolves phone numbers using a priority waterfall:
    /// 1. CallerInfoPhone (only for Phone/Other/ePA sources)
    /// 2. Member phones in order: Home → Cell → Work (deduplicated, empty filtered)
    /// 3. Medication history phone lookup (fallback when no member phones)
    /// </summary>
    private async Task<(string? HomePhone, string? CellPhone, string? WorkPhone)> ResolvePhoneNumbersAsync(
        TcnInformationEvent @event, CancellationToken cancellationToken)
    {
        if (CallerInfoPhoneSources.Contains(@event.EpisodeSourceType ?? string.Empty)
            && !string.IsNullOrWhiteSpace(@event.CallerInfoPhone))
        {
            Logger.LogInformation($"--- Using CallerInfoPhone for EpisodeId: {@event.EpisodeNumber}, Source: {@event.EpisodeSourceType} ---");
            return (@event.CallerInfoPhone, null, null);
        }

        var memberPhones = GetOrderedMemberPhones(@event);
        if (memberPhones.HomePhone != null)
            return memberPhones;

        Logger.LogInformation($"--- Falling back to medication history for EpisodeId: {@event.EpisodeNumber} ---");
        var medHistoryPhone = await GetMemberPhoneFromMedicationHistoryAsync(@event.SubscriberNumber, @event.PersonCode, cancellationToken);
        if (!string.IsNullOrWhiteSpace(medHistoryPhone))
        {
            Logger.LogInformation($"--- Using medication history phone for EpisodeId: {@event.EpisodeNumber} ---");
            return (medHistoryPhone, null, null);
        }

        return (null, null, null);
    }

    /// <summary>
    /// Returns member phones in priority order (Home → Cell → Work), deduplicated and filtered.
    /// </summary>
    public static (string? HomePhone, string? CellPhone, string? WorkPhone) GetOrderedMemberPhones(TcnInformationEvent @event)
    {
        var uniquePhones = new[] { @event.MemberHomePhone, @event.MemberCellPhone, @event.MemberWorkPhone }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (uniquePhones.Count == 0)
            return (null, null, null);

        return (
            uniquePhones.ElementAtOrDefault(0),
            uniquePhones.ElementAtOrDefault(1),
            uniquePhones.ElementAtOrDefault(2)
        );
    }

    private async Task<string?> GetMemberPhoneFromMedicationHistoryAsync(string? subscriberNumber, string? personCode, CancellationToken cancellationToken)
    {
        if (_medicationHistoryServiceClient == null)
        {
            Logger.LogWarning("--- MedicationHistoryServiceClient is not configured, cannot look up member phone for fax source ---");
            return null;
        }

        if (string.IsNullOrWhiteSpace(subscriberNumber) || string.IsNullOrWhiteSpace(personCode))
        {
            Logger.LogWarning("--- Missing subscriberNumber or personCode for medication history phone lookup ---");
            return null;
        }

        try
        {
            var response = await _medicationHistoryServiceClient.MemberAsync(
                startDate: null,
                endDate: null,
                claimStatuses: null,
                new MedicationHistoryRequest
                {
                    SubscriberNumber = subscriberNumber,
                    PersonCode = personCode
                },
                cancellationToken);

            var memberPhone = response.Result?
                .Where(t => !string.IsNullOrWhiteSpace(t.MemberPhone))
                .OrderByDescending(t => t.EnteredDate)
                .Take(5)
                .FirstOrDefault()?.MemberPhone;

            Logger.LogInformation($"--- Medication history phone lookup result: {(string.IsNullOrWhiteSpace(memberPhone) ? "not found" : "found")} ---");
            return memberPhone;
        }
        catch (Exception ex)
        {
            Logger.LogError($"--- Failed to look up member phone from medication history: {ex.Message} ---");
            return null;
        }
    }
}
