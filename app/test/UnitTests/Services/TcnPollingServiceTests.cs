using Microsoft.Extensions.Configuration;
using NSubstitute;
using ServiceClients.Navitus;
using Services.Implementation;
using Shared.Authorization;
using Shared.Services.Auth;
using IntegrationPriorAuth = ServiceClients.Navitus.IntegrationPriorAuth;
using Xunit;

namespace UnitTests.Services;

public class TcnPollingServiceTests
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;
    private readonly IntegrationPriorAuth.IIntegrationPriorAuthServiceClient _integrationPriorAuthServiceClient;
    private readonly IConfiguration _configuration;
    private readonly TcnPollingService _tcnPollingService;

    public TcnPollingServiceTests()
    {
        _authProvider = Substitute.For<IAuthProvider>();
        _umServiceClient = Substitute.For<IUMServiceClient>();
        _integrationPriorAuthServiceClient = Substitute.For<IntegrationPriorAuth.IIntegrationPriorAuthServiceClient>();
        _configuration = Substitute.For<IConfiguration>();

        _configuration.GetValue<string>("NavitusUtilizationManagementBaseURL").Returns("http://test-url.com");
        _configuration.GetValue<string>("IntegrationPriorAuthBaseURL").Returns("http://test-pa-url.com");

        _umServiceClient.CreateEpisodeNoteAsync(Arg.Any<CreateEpisodeNoteRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<CreateEpisodeNoteResponse>(200, new Dictionary<string, IEnumerable<string>>(), new CreateEpisodeNoteResponse())));

        _tcnPollingService = new TcnPollingService(_authProvider, _umServiceClient, _integrationPriorAuthServiceClient, _configuration);
    }

    [Fact]
    public async Task EnsureAuthenticated_ShouldSetBearerTokens()
    {
        // Arrange
        string expectedToken = "test-token";
        _authProvider.AuthenticateInternallyNucleus(Arg.Any<string[]>()).Returns(expectedToken);

        // Act
        await _tcnPollingService.EnsureAuthenticated();

        // Assert
        await _authProvider
            .Received(1)
            .AuthenticateInternallyNucleus(
                Arg.Is<string[]>(scopes => scopes.Contains(Scopes.Client.NucleusNavitus) && scopes.Contains(Scopes.PriorAuthEpisode.Read))
            );
        Assert.Equal(expectedToken, _umServiceClient.BearerToken);
        Assert.Equal(expectedToken, _integrationPriorAuthServiceClient.BearerToken);
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_WithNoPendingDetails_ShouldReturnEarly()
    {
        // Arrange
        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = new List<PendingTcnCallDetail>() }
            )));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert
        await _integrationPriorAuthServiceClient.DidNotReceive().GetFtpReportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _umServiceClient.DidNotReceive().UpdateCallDetailAsync(Arg.Any<UpdateTcnCallDetailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_WithAnsweredResult_ShouldNotSendFailureNotification()
    {
        // Arrange
        var pendingDetails = new List<PendingTcnCallDetail>
        {
            new() { TcnTaskSid = "task-789", EpisodeNumber = 100003 }
        };

        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = pendingDetails }
            )));

        _integrationPriorAuthServiceClient.GetFtpReportAsync("task-789", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "100003", ShortResult = "Answered", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 60 }
                    }
                })));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert
        await _umServiceClient.Received(1).UpdateCallDetailAsync(Arg.Is<UpdateTcnCallDetailRequest>(r => r.CallResult == "Answered"), Arg.Any<CancellationToken>());
        await _umServiceClient.Received(1).CreateEpisodeNoteAsync(Arg.Is<CreateEpisodeNoteRequest>(r => r.SubjectLine == "TCN Call Attempt"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_WithPendingResult_ShouldCreateNoteButNotSendFailureNotification()
    {
        // Arrange
        var pendingDetails = new List<PendingTcnCallDetail>
        {
            new() { TcnTaskSid = "task-999", EpisodeNumber = 100004 }
        };

        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = pendingDetails }
            )));

        _integrationPriorAuthServiceClient.GetFtpReportAsync("task-999", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "100004", ShortResult = "Pending" }
                    }
                })));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert - should create a note for pending result but not send failure notification
        await _umServiceClient.Received(1).CreateEpisodeNoteAsync(Arg.Is<CreateEpisodeNoteRequest>(r => r.SubjectLine == "TCN Call Attempt"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_WhenExceptionOccurs_ShouldContinueProcessingOtherDetails()
    {
        // Arrange
        var pendingDetails = new List<PendingTcnCallDetail>
        {
            new() { TcnTaskSid = "task-fail", EpisodeNumber = 100006 },
            new() { TcnTaskSid = "task-success", EpisodeNumber = 100007 }
        };

        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = pendingDetails }
            )));

        _integrationPriorAuthServiceClient
            .When(x => x.GetFtpReportAsync("task-fail", Arg.Any<CancellationToken>()))
            .Do(x => throw new Exception("Test error"));

        _integrationPriorAuthServiceClient.GetFtpReportAsync("task-success", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "100007", ShortResult = "Answered", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 30 }
                    }
                })));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert - should still process the second call detail
        await _umServiceClient.Received(1).UpdateCallDetailAsync(Arg.Is<UpdateTcnCallDetailRequest>(r => r.TcnTaskSid == "task-success"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_WithFailureResult_ShouldCreateEmailMetadataDirectly()
    {
        // Arrange
        var distributionListSection = Substitute.For<IConfigurationSection>();
        distributionListSection.Value.Returns("pa-team@navitus.com");
        _configuration.GetSection("TcnFailureNotificationDistributionList").Returns(distributionListSection);

        var pendingDetails = new List<PendingTcnCallDetail>
        {
            new() { TcnTaskSid = "task-111", EpisodeNumber = 100008 }
        };

        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = pendingDetails }
            )));

        _integrationPriorAuthServiceClient.GetFtpReportAsync("task-111", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "100008", ShortResult = "No Answer", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 0 }
                    }
                })));

        _umServiceClient.Episode_CreateEmailMetadataAsync(Arg.Any<CreateEmailMetadataRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse(200, new Dictionary<string, IEnumerable<string>>())));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert
        await _umServiceClient.Received(1).Episode_CreateEmailMetadataAsync(
            Arg.Is<CreateEmailMetadataRequest>(r =>
                r.NavitusEpisodeId == 100008 &&
                r.Subject == "Unsuccessful Oral Notification" &&
                r.Recipients.Contains("pa-team@navitus.com")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_WithMultipleFailures_ShouldSendSeparateEmailPerEpisode()
    {
        // Arrange
        var distributionListSection = Substitute.For<IConfigurationSection>();
        distributionListSection.Value.Returns("pa-team@navitus.com");
        _configuration.GetSection("TcnFailureNotificationDistributionList").Returns(distributionListSection);

        var pendingDetails = new List<PendingTcnCallDetail>
        {
            new() { TcnTaskSid = "task-201", EpisodeNumber = 100010 },
            new() { TcnTaskSid = "task-202", EpisodeNumber = 100011 }
        };

        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = pendingDetails }
            )));

        _integrationPriorAuthServiceClient.GetFtpReportAsync("task-201", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "100010", ShortResult = "Busy", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 0 }
                    }
                })));

        _integrationPriorAuthServiceClient.GetFtpReportAsync("task-202", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "100011", ShortResult = "No Answer", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 0 }
                    }
                })));

        _umServiceClient.Episode_CreateEmailMetadataAsync(Arg.Any<CreateEmailMetadataRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse(200, new Dictionary<string, IEnumerable<string>>())));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert - one email per failed episode, each referencing only its own episode
        await _umServiceClient.Received(1).Episode_CreateEmailMetadataAsync(
            Arg.Is<CreateEmailMetadataRequest>(r => r.NavitusEpisodeId == 100010 && !r.HtmlBody.Contains("100011")),
            Arg.Any<CancellationToken>());
        await _umServiceClient.Received(1).Episode_CreateEmailMetadataAsync(
            Arg.Is<CreateEmailMetadataRequest>(r => r.NavitusEpisodeId == 100011 && !r.HtmlBody.Contains("100010")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollPendingCallDetailsAsync_TwoContactsShareOneCampaignTaskSid_EachMatchedToOwnResult()
    {
        // Arrange - under the Lead Drip pattern, a single campaign TaskSid's report can contain
        // results for every contact dropped into it. Blindly taking the first result (the old bug)
        // would apply one contact's outcome to every pending detail sharing that TaskSid.
        var pendingDetails = new List<PendingTcnCallDetail>
        {
            new() { TcnTaskSid = "shared-campaign-task", EpisodeNumber = 400001 },
            new() { TcnTaskSid = "shared-campaign-task", EpisodeNumber = 400002 }
        };

        _umServiceClient.GetPendingCallDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ApiResponse<GetPendingTcnCallDetailsResponse>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new GetPendingTcnCallDetailsResponse { PendingCallDetails = pendingDetails }
            )));

        // Same report returned for both polls (same shared TaskSid), containing both contacts' results.
        _integrationPriorAuthServiceClient.GetFtpReportAsync("shared-campaign-task", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntegrationPriorAuth.ApiResponse<IntegrationPriorAuth.FtpReportResult>(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                new IntegrationPriorAuth.FtpReportResult
                {
                    CallResults = new List<IntegrationPriorAuth.FtpCallResult>
                    {
                        new() { EpisodeNumber = "400001", ShortResult = "Answered", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 60 },
                        new() { EpisodeNumber = "400002", ShortResult = "Busy", LastAttemptedAt = DateTimeOffset.UtcNow, DurationSeconds = 0 }
                    }
                })));

        // Act
        await _tcnPollingService.PollPendingCallDetailsAsync(10, CancellationToken.None);

        // Assert - each episode gets its own result, not both getting the first one in the list
        await _umServiceClient.Received(1).UpdateCallDetailAsync(
            Arg.Is<UpdateTcnCallDetailRequest>(r => r.EpisodeNumber == 400001 && r.CallResult == "Answered"),
            Arg.Any<CancellationToken>());
        await _umServiceClient.Received(1).UpdateCallDetailAsync(
            Arg.Is<UpdateTcnCallDetailRequest>(r => r.EpisodeNumber == 400002 && r.CallResult == "Busy"),
            Arg.Any<CancellationToken>());
    }
}
