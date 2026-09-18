using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using Models.Event;
using NSubstitute;
using Services.Interface;
using TcnOralNotificationProcessor;
using Xunit;

namespace UnitTests;

public class TcnOralNotificationSqsRecordHandlerTests
{
    private readonly ITcnOralNotificationService _mockTcnOralNotificationService;
    private readonly TcnOralNotificationSqsRecordHandler _handler;

    public TcnOralNotificationSqsRecordHandlerTests()
    {
        _mockTcnOralNotificationService = Substitute.For<ITcnOralNotificationService>();
        _handler = new TcnOralNotificationSqsRecordHandler(_mockTcnOralNotificationService);
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_ShouldCallScheduleBroadcast()
    {
        // Arrange
        var @event = new TcnInformationEvent
        {
            EpisodeNumber = 123,
            MemberFirstName = "Jane",
            MemberLastName = "Doe",
            PriorAuthStatus = "Approved",
            EpisodeSourceType = "Phone",
            CallerInfoPhone = "555-123-4567",
            DrugDescription = "Test Drug"
        };

        var cloudWatchEvent = new CloudWatchEvent<TcnInformationEvent>
        {
            DetailType = nameof(TcnInformationEvent),
            Detail = @event
        };

        var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

        // Act
        var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

        // Assert
        await _mockTcnOralNotificationService
            .Received(1)
            .ScheduleBroadcastAsync(
                Arg.Is<TcnInformationEvent>(e => e.EpisodeNumber == @event.EpisodeNumber),
                Arg.Any<CancellationToken>()
            );

        Assert.Equal(RecordHandlerResult.None, result);
    }

    [Fact]
    public async Task HandleAsync_WithNoPhoneNumbers_ShouldStillDelegateToService()
    {
        // Arrange - service now handles phone resolution including claims fallback
        var @event = new TcnInformationEvent
        {
            EpisodeNumber = 123,
            MemberFirstName = "Jane",
            MemberLastName = "Doe",
            MemberHomePhone = string.Empty,
            MemberCellPhone = string.Empty,
            MemberWorkPhone = string.Empty
        };

        var cloudWatchEvent = new CloudWatchEvent<TcnInformationEvent>
        {
            DetailType = nameof(TcnInformationEvent),
            Detail = @event
        };

        var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

        // Act
        var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

        // Assert - handler no longer checks phones, delegates decision to service
        await _mockTcnOralNotificationService
            .Received(1)
            .ScheduleBroadcastAsync(Arg.Any<TcnInformationEvent>(), Arg.Any<CancellationToken>());

        Assert.Equal(RecordHandlerResult.None, result);
    }

    [Fact]
    public async Task HandleAsync_WithFaxSourceAndNoCallerPhone_ShouldStillDelegateToService()
    {
        // Arrange
        var @event = new TcnInformationEvent
        {
            EpisodeNumber = 123,
            PriorAuthStatus = "Denied",
            EpisodeSourceType = "Fax"
        };

        var cloudWatchEvent = new CloudWatchEvent<TcnInformationEvent>
        {
            DetailType = nameof(TcnInformationEvent),
            Detail = @event
        };

        var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

        // Act
        var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

        // Assert - handler always delegates to service regardless of source/phone
        await _mockTcnOralNotificationService
            .Received(1)
            .ScheduleBroadcastAsync(Arg.Any<TcnInformationEvent>(), Arg.Any<CancellationToken>());

        Assert.Equal(RecordHandlerResult.None, result);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidBody_ShouldThrowException()
    {
        // Arrange
        var sqsMessage = new SQSEvent.SQSMessage { Body = "not valid json" };

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _handler.HandleAsync(sqsMessage, CancellationToken.None));
    }
}
