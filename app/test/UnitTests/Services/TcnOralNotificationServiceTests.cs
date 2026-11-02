using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Models.Event;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ServiceClients.Navitus;
using ServiceClients.Navitus.IntegrationPriorAuth;
using ServiceClients.Navitus.MedicationHistory;
using Services.Implementation;
using Shared.Services.Auth;
using Xunit;
using MedHistoryApiResponse = ServiceClients.Navitus.MedicationHistory.ApiResponse<System.Collections.Generic.ICollection<ServiceClients.Navitus.MedicationHistory.Transaction>>;
using IntPaApiResponse = ServiceClients.Navitus.IntegrationPriorAuth.ApiResponse<ServiceClients.Navitus.IntegrationPriorAuth.ScheduleBroadcastResponse>;

namespace UnitTests.Services;

public class TcnOralNotificationServiceTests
{
    private readonly IAuthProvider _mockAuthProvider;
    private readonly IUMServiceClient _mockUmServiceClient;
    private readonly IIntegrationPriorAuthServiceClient _mockIntegrationPriorAuthServiceClient;
    private readonly IMedicationHistoryServiceClient _mockMedicationHistoryServiceClient;
    private readonly IConfiguration _configuration;
    private readonly TcnOralNotificationService _service;

    public TcnOralNotificationServiceTests()
    {
        _mockAuthProvider = Substitute.For<IAuthProvider>();
        _mockUmServiceClient = Substitute.For<IUMServiceClient>();
        _mockIntegrationPriorAuthServiceClient = Substitute.For<IIntegrationPriorAuthServiceClient>();
        _mockMedicationHistoryServiceClient = Substitute.For<IMedicationHistoryServiceClient>();

        var configValues = new Dictionary<string, string?>
        {
            { "NavitusUtilizationManagementBaseURL", "https://um-api-test" },
            { "IntegrationPriorAuthBaseURL", "https://integration-pa-test" },
            { "TCN_SCHEDULE_TEMPLATE", "1" },
            { "TCN_IMPORT_TEMPLATE", "2" },
            { "TCN_DESCRIPTION", "Test TCN" },
            { "TCN_TIMEZONE", "CST" },
            { "TCN_DIAL_ORDER", "HCW" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _service = new TcnOralNotificationService(
            _mockAuthProvider,
            _mockUmServiceClient,
            _mockIntegrationPriorAuthServiceClient,
            _configuration,
            _mockMedicationHistoryServiceClient);
    }

    #region Status Filtering Tests

    [Theory]
    [InlineData("Approved")]
    [InlineData("Denied")]
    [InlineData("PartiallyApproved")]
    [InlineData("Dismissed")]
    [InlineData("Excluded")]
    [InlineData("ReopenApproved")]
    [InlineData("ReopenDenied")]
    [InlineData("PartiallyFavorable")]
    [InlineData("FullyFavorable")]
    [InlineData("Withdrawn")]
    public async Task ScheduleBroadcastAsync_WithEligibleStatus_ShouldSchedule(string status)
    {
        // Arrange
        var @event = CreateEvent(status, "Phone", callerInfoPhone: "555-111-2222");
        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(Arg.Any<ScheduleBroadcastRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("InProgress")]
    [InlineData("Abandoned")]
    [InlineData("FormFaxedToProvider")]
    [InlineData("FollowupNeeded")]
    [InlineData("ePAAwaitingAnswers")]
    [InlineData("Postponed")]
    [InlineData("Preapproval")]
    [InlineData("ReceivedInError")]
    public async Task ScheduleBroadcastAsync_WithIneligibleStatus_ShouldSkip(string status)
    {
        // Arrange
        var @event = CreateEvent(status, "Phone", callerInfoPhone: "555-111-2222");

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert
        await _mockIntegrationPriorAuthServiceClient
            .DidNotReceive()
            .ScheduleBroadcastAsync(Arg.Any<ScheduleBroadcastRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Phone Resolution Hierarchy - Priority 1: CallerInfoPhone

    [Theory]
    [InlineData("Phone")]
    [InlineData("Other")]
    [InlineData("ePA")]
    public async Task ScheduleBroadcastAsync_WithCallerInfoSource_ShouldUseCallerInfoPhone(string sourceType)
    {
        // Arrange
        var @event = CreateEvent("Approved", sourceType, callerInfoPhone: "555-999-8888", subscriberNumber: "SUB1", personCode: "01");
        @event.MemberHomePhone = "555-000-0000"; // should NOT be used
        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - CallerInfoPhone takes priority
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-999-8888"),
                Arg.Any<CancellationToken>());

        // Should not call med history since CallerInfoPhone was available
        await _mockMedicationHistoryServiceClient
            .DidNotReceive()
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>());
    }

    #endregion

    #region Phone Resolution Hierarchy - Priority 2: Medication History

    [Theory]
    [InlineData("Phone")]
    [InlineData("Other")]
    [InlineData("ePA")]
    [InlineData("Fax")]
    public async Task ScheduleBroadcastAsync_NoCallerInfoPhone_ShouldFallToMedHistory(string sourceType)
    {
        // Arrange
        var @event = CreateEvent("Approved", sourceType, callerInfoPhone: null, subscriberNumber: "SUB123", personCode: "01");

        var transactions = new List<Transaction>
        {
            new() { MemberPhone = "555-MED-HIST" }
        };

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MedHistoryApiResponse(200, new Dictionary<string, IEnumerable<string>>(), transactions));

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - med history phone used
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-MED-HIST"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleBroadcastAsync_WithFaxSource_ShouldLookupMemberPhoneFromMedicationHistory()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Fax", subscriberNumber: "SUB123", personCode: "01");

        var transactions = new List<Transaction>
        {
            new() { MemberPhone = "555-777-6666" },
            new() { MemberPhone = "555-888-9999" }
        };

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MedHistoryApiResponse(200, new Dictionary<string, IEnumerable<string>>(), transactions));

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - first available phone from last 5 claims
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-777-6666"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleBroadcastAsync_WithFaxSource_FirstClaimNoPhone_ShouldUseNextAvailable()
    {
        // Arrange
        var @event = CreateEvent("Denied", "Fax", subscriberNumber: "SUB123", personCode: "01");

        var transactions = new List<Transaction>
        {
            new() { MemberPhone = null },
            new() { MemberPhone = "" },
            new() { MemberPhone = "555-444-3333" },
            new() { MemberPhone = "555-222-1111" }
        };

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MedHistoryApiResponse(200, new Dictionary<string, IEnumerable<string>>(), transactions));

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-444-3333"),
                Arg.Any<CancellationToken>());
    }

    #endregion

    #region Phone Resolution Hierarchy - Priority 3: Fallback to TCN Phone Fields

    [Fact]
    public async Task ScheduleBroadcastAsync_NoCallerInfo_NoMedHistory_ShouldFallbackToMemberHomePhone()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Fax", subscriberNumber: "SUB123", personCode: "01");
        @event.MemberHomePhone = "555-FALLBACK";

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MedHistoryApiResponse(200, new Dictionary<string, IEnumerable<string>>(), new List<Transaction>()));

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - falls back to MemberHomePhone
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-FALLBACK"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleBroadcastAsync_NoCallerInfo_NoMedHistory_ShouldFallbackToCellPhone()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Phone", subscriberNumber: "SUB123", personCode: "01");
        @event.MemberHomePhone = null;
        @event.MemberCellPhone = "555-CELL";

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MedHistoryApiResponse(200, new Dictionary<string, IEnumerable<string>>(), new List<Transaction>()));

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-CELL"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleBroadcastAsync_NoPhoneAnywhere_ShouldSkip()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Fax", subscriberNumber: "SUB123", personCode: "01");
        @event.MemberHomePhone = null;
        @event.MemberCellPhone = null;
        @event.MemberWorkPhone = null;

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new MedHistoryApiResponse(200, new Dictionary<string, IEnumerable<string>>(), new List<Transaction>()));

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - no phone resolved at any level, skip TCN
        await _mockIntegrationPriorAuthServiceClient
            .DidNotReceive()
            .ScheduleBroadcastAsync(Arg.Any<ScheduleBroadcastRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Phone Resolution - Error Handling

    [Fact]
    public async Task ScheduleBroadcastAsync_MedicationHistoryThrows_ShouldFallbackToTcnPhoneFields()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Fax", subscriberNumber: "SUB123", personCode: "01");
        @event.MemberHomePhone = "555-FALLBACK";

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Service unavailable"));

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - med history failed, falls back to TCN phone fields
        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-FALLBACK"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleBroadcastAsync_MedicationHistoryThrows_NoFallbackPhone_ShouldSkip()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Fax", subscriberNumber: "SUB123", personCode: "01");
        @event.MemberHomePhone = null;
        @event.MemberCellPhone = null;
        @event.MemberWorkPhone = null;

        _mockMedicationHistoryServiceClient
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert
        await _mockIntegrationPriorAuthServiceClient
            .DidNotReceive()
            .ScheduleBroadcastAsync(Arg.Any<ScheduleBroadcastRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleBroadcastAsync_MissingSubscriberNumber_ShouldFallbackToTcnPhoneFields()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Fax", subscriberNumber: null, personCode: "01");
        @event.MemberHomePhone = "555-FALLBACK";

        SetupSuccessfulBroadcast();

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert - can't call med history, falls back to TCN phone fields
        await _mockMedicationHistoryServiceClient
            .DidNotReceive()
            .MemberAsync(
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<ClaimStatuses?>(),
                Arg.Any<MedicationHistoryRequest>(),
                Arg.Any<CancellationToken>());

        await _mockIntegrationPriorAuthServiceClient
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<ScheduleBroadcastRequest>(r => r.MemberHomePhone == "555-FALLBACK"),
                Arg.Any<CancellationToken>());
    }

    #endregion

    #region Call Detail Creation

    [Fact]
    public async Task ScheduleBroadcastAsync_AfterSuccessfulBroadcast_ShouldCreateCallDetail()
    {
        // Arrange
        var @event = CreateEvent("Approved", "Phone", callerInfoPhone: "555-111-2222");
        SetupSuccessfulBroadcast("TASK-SID-123");

        // Act
        await _service.ScheduleBroadcastAsync(@event, CancellationToken.None);

        // Assert
        await _mockUmServiceClient
            .Received(1)
            .CreateCallDetailAsync(
                Arg.Is<CreateTcnCallDetailRequest>(r =>
                    r.TcnTaskSid == "TASK-SID-123" && r.EpisodeNumber == @event.EpisodeNumber),
                Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static TcnInformationEvent CreateEvent(
        string status,
        string sourceType,
        string? callerInfoPhone = null,
        string? subscriberNumber = null,
        string? personCode = null)
    {
        return new TcnInformationEvent
        {
            EpisodeNumber = 12345,
            ExternalEpisodeId = "EXT-123",
            PriorAuthStatus = status,
            EpisodeSourceType = sourceType,
            CallerInfoPhone = callerInfoPhone,
            SubscriberNumber = subscriberNumber,
            PersonCode = personCode,
            MemberFirstName = "John",
            MemberLastName = "Smith",
            UniqueId = "UNIQUE-123",
            DateOfBirth = "1990-01-01",
            CarrierId = "CARRIER-1",
            DrugDescription = "Test Drug",
            MemberZipCode = "12345",
            MemberLanguageCode = "EN"
        };
    }

    private void SetupSuccessfulBroadcast(string taskSid = "TASK-SID-DEFAULT")
    {
        _mockIntegrationPriorAuthServiceClient
            .ScheduleBroadcastAsync(Arg.Any<ScheduleBroadcastRequest>(), Arg.Any<CancellationToken>())
            .Returns(new IntPaApiResponse(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new ScheduleBroadcastResponse { TaskSid = taskSid, Success = true }));
    }

    #endregion
}
