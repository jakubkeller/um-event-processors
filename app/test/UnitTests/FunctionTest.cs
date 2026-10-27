using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ServiceClients.Navitus;
using Services.Interface;
using Shared.Services.Auth;
using Xunit;

namespace UnitTests;

public class FunctionTest : BaseTests
{
    [Fact]
    public async Task NoRecord()
    {
        var _mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();

        var mockServices = new ServiceCollection();

        mockServices.AddSingleton((services) => _mockTemplatedNoteService);
        mockServices.AddSingleton<ISqsBatchProcessor, TemplatedNoteProcessor.SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessor.TemplatedNoteProcessorSqsRecordHandler>();

        var mockProvider = mockServices.BuildServiceProvider();

        List<SQSEvent.SQSMessage> SqsMessages = [];

        var request = new SQSEvent { Records = SqsMessages };
        var context = new TestLambdaContext();
        var function = new TemplatedNoteProcessor.Function(mockProvider);

        Func<Task> action = async () => await function.FunctionHandler(request, context);

        // If no records are supplied (unrealistic scenario for AWS Lambda polling an SQS Queue), an exception will occur, and no records will be deleted from the queue
        _ = await action.Should().ThrowAsync<AggregateException>();
    }

    [Fact]
    public async Task OneValidRecord_Should_ProcessSuccessfully()
    {
        var _mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();

        var mockServices = new ServiceCollection();

        mockServices.AddSingleton((services) => _mockTemplatedNoteService);
        mockServices.AddSingleton<ISqsBatchProcessor, TemplatedNoteProcessor.SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessor.TemplatedNoteProcessorSqsRecordHandler>();

        var mockProvider = mockServices.BuildServiceProvider();

        List<SQSEvent.SQSMessage> SqsMessages =
        [
            new SQSEvent.SQSMessage
            {
                MessageId = "1",
                Body =
                    "{\"id\":\"abc\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"EpisodeCreated\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
        ];

        var request = new SQSEvent { Records = SqsMessages };
        var context = new TestLambdaContext();
        var function = new TemplatedNoteProcessor.Function(mockProvider);

        var result = await function.FunctionHandler(request, context);

        result.BatchItemFailures.Count.Should().Be(0);
    }

    [Fact]
    public async Task MultipleValidRecord_Should_ProcessSuccessfully()
    {
        var _mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();

        var mockServices = new ServiceCollection();

        mockServices.AddSingleton((services) => _mockTemplatedNoteService);
        mockServices.AddSingleton<ISqsBatchProcessor, TemplatedNoteProcessor.SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessor.TemplatedNoteProcessorSqsRecordHandler>();

        var mockProvider = mockServices.BuildServiceProvider();

        List<SQSEvent.SQSMessage> SqsMessages =
        [
            new SQSEvent.SQSMessage
            {
                MessageId = "1",
                Body =
                    "{\"id\":\"abc\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"EpisodeCreated\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            new SQSEvent.SQSMessage
            {
                MessageId = "2",
                Body =
                    "{\"id\":\"def\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"EpisodeUpdatedV2\",\"detail\":{\"oldEpisode\":{}, \"newEpisode\": {}},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            new SQSEvent.SQSMessage
            {
                MessageId = "3",
                Body =
                    "{\"id\":\"ghi\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"GrievanceAndAppealsEpisodeCreated\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            new SQSEvent.SQSMessage
            {
                MessageId = "4",
                Body =
                    "{\"id\":\"jkl\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"GrievanceAndAppealsEpisodeUpdated\",\"detail\":{\"oldEpisode\":{}, \"newEpisode\": {}},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
        ];

        var request = new SQSEvent { Records = SqsMessages };
        var context = new TestLambdaContext();
        var function = new TemplatedNoteProcessor.Function(mockProvider);

        var result = await function.FunctionHandler(request, context);

        result.BatchItemFailures.Count.Should().Be(0);
    }

    [Fact]
    public async Task MultipleValidRecordWithOneInvalidRecord_Should_ProcessBatchSuccessfullyAndReportOneFailure()
    {
        var _mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();

        var mockServices = new ServiceCollection();

        mockServices.AddSingleton((services) => _mockTemplatedNoteService);
        mockServices.AddSingleton<ISqsBatchProcessor, TemplatedNoteProcessor.SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessor.TemplatedNoteProcessorSqsRecordHandler>();

        var mockProvider = mockServices.BuildServiceProvider();

        var invalidEventId = "3";

        List<SQSEvent.SQSMessage> SqsMessages =
        [
            // Valid EpisodeCreated Event
            new SQSEvent.SQSMessage
            {
                MessageId = "1",
                Body =
                    "{\"id\":\"abc\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"EpisodeCreated\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            // Valid EpisodeCreated Event
            new SQSEvent.SQSMessage
            {
                MessageId = "2",
                Body =
                    "{\"id\":\"def\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"EpisodeCreated\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            // Invalid SomeInvalidEventType Event
            new SQSEvent.SQSMessage
            {
                MessageId = invalidEventId,
                Body =
                    "{\"id\":\"hij\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"SomeInvalidEventType\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            // Valid EpisodeUpdatedV2 Event
            new SQSEvent.SQSMessage
            {
                MessageId = "4",
                Body =
                    "{\"id\":\"hij\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"EpisodeUpdatedV2\",\"detail\":{\"oldEpisode\":{}, \"newEpisode\": {}},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            // Valid GrievanceAndAppealsEpisodeCreated Event
            new SQSEvent.SQSMessage
            {
                MessageId = "5",
                Body =
                    "{\"id\":\"def\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"GrievanceAndAppealsEpisodeCreated\",\"detail\":{},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
            // Valid GrievanceAndAppealsEpisodeUpdated Event
            new SQSEvent.SQSMessage
            {
                MessageId = "6",
                Body =
                    "{\"id\":\"hij\",\"time\":\"2024-08-01T15:04:46Z\",\"source\":\"com.navitus.um\",\"version\":\"1\",\"account\":\"111111111111\",\"region\":\"us-east-1\",\"detail-type\":\"GrievanceAndAppealsEpisodeUpdated\",\"detail\":{\"oldEpisode\":{}, \"newEpisode\": {}},\"Resources\":[]}",
                EventSourceArn = "arn:aws:sqs:us-east-2:123456789012:my-queue"
            },
        ];

        var request = new SQSEvent { Records = SqsMessages };
        var context = new TestLambdaContext();
        var function = new TemplatedNoteProcessor.Function(mockProvider);

        var result = await function.FunctionHandler(request, context);

        result.BatchItemFailures.Count.Should().Be(1);
        result.BatchItemFailures[0].ItemIdentifier.Should().Be(invalidEventId);
    }
}

public class RtpbReceiveFunctionTests
{
    private static IServiceProvider BuildProvider(IRtpbReportingStorageService storageService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(storageService);
        services.AddSingleton<AWS.Lambda.Powertools.BatchProcessing.Sqs.ISqsBatchProcessor, RtpbReceiveProcessor.SqsBatchProcessorWithLogging>();
        services.AddSingleton<AWS.Lambda.Powertools.BatchProcessing.Sqs.ISqsRecordHandler, RtpbReceiveProcessor.RtpbReceiveSqsRecordHandler>();
        return services.BuildServiceProvider();
    }

    private static SQSEvent.SQSMessage BuildMessage(string messageId, string detailType, object payload) =>
        new()
        {
            MessageId = messageId,
            Body = System.Text.Json.JsonSerializer.Serialize(new
            {
                version = "0",
                id = "test-id",
                source = "com.navitus.um",
                account = "123456789012",
                region = "us-east-1",
                time = "2025-01-07T20:53:38Z",
                detail_type = detailType,  // note: CloudWatchEvent uses "detail-type" via JsonPropertyName
                resources = Array.Empty<string>()
            }),
            EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
        };

    private static SQSEvent.SQSMessage BuildRtpbMessage(string messageId = "1") =>
        new()
        {
            MessageId = messageId,
            Body = "{\"version\":\"0\",\"id\":\"a43a861a\",\"detail-type\":\"RtpbReceiveEvent\",\"source\":\"com.navitus.um\",\"account\":\"123456789012\",\"time\":\"2025-01-07T20:53:38Z\",\"region\":\"us-east-1\",\"resources\":[],\"detail\":{\"payload\":{\"request\":\"<req/>\",\"response\":\"<res/>\",\"messageID\":\"11111111-2222-3333-4444-555555555555\",\"initiationTime\":\"01-07-2025 14:53:38 CT\",\"responseTime\":\"01-07-2025 14:53:39 CT\"}}}",
            EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
        };

    [Fact]
    public async Task OneValidRecord_Should_ProcessSuccessfully()
    {
        var mockStorage = Substitute.For<IRtpbReportingStorageService>();
        var function = new RtpbReceiveProcessor.Function(BuildProvider(mockStorage));

        var result = await function.FunctionHandler(
            new SQSEvent { Records = [BuildRtpbMessage("1")] },
            new TestLambdaContext());

        result.BatchItemFailures.Count.Should().Be(0);
        await mockStorage.Received(1).StoreRtpbTransactionAsync(Arg.Any<Models.Event.RtpbReceiveEvent>());
    }

    [Fact]
    public async Task MultipleValidRecords_Should_ProcessAllSuccessfully()
    {
        var mockStorage = Substitute.For<IRtpbReportingStorageService>();
        var function = new RtpbReceiveProcessor.Function(BuildProvider(mockStorage));

        var result = await function.FunctionHandler(
            new SQSEvent { Records = [BuildRtpbMessage("1"), BuildRtpbMessage("2"), BuildRtpbMessage("3")] },
            new TestLambdaContext());

        result.BatchItemFailures.Count.Should().Be(0);
        await mockStorage.Received(3).StoreRtpbTransactionAsync(Arg.Any<Models.Event.RtpbReceiveEvent>());
    }

    [Fact]
    public async Task InvalidDetailType_Should_ReportBatchItemFailure()
    {
        var mockStorage = Substitute.For<IRtpbReportingStorageService>();
        var function = new RtpbReceiveProcessor.Function(BuildProvider(mockStorage));

        var invalidMessage = new SQSEvent.SQSMessage
        {
            MessageId = "bad-1",
            Body = "{\"version\":\"0\",\"id\":\"x\",\"detail-type\":\"UnknownEvent\",\"source\":\"com.navitus.um\",\"account\":\"123456789012\",\"time\":\"2025-01-07T20:53:38Z\",\"region\":\"us-east-1\",\"resources\":[],\"detail\":{}}",
            EventSourceArn = "arn:aws:sqs:us-east-1:123456789012:test-queue"
        };

        var result = await function.FunctionHandler(
            new SQSEvent { Records = [BuildRtpbMessage("1"), invalidMessage, BuildRtpbMessage("2")] },
            new TestLambdaContext());

        result.BatchItemFailures.Count.Should().Be(1);
        result.BatchItemFailures[0].ItemIdentifier.Should().Be("bad-1");
    }

    [Fact]
    public async Task NoRecords_Should_ThrowAggregateException()
    {
        var mockStorage = Substitute.For<IRtpbReportingStorageService>();
        var function = new RtpbReceiveProcessor.Function(BuildProvider(mockStorage));

        Func<Task> act = async () => await function.FunctionHandler(
            new SQSEvent { Records = [] },
            new TestLambdaContext());

        await act.Should().ThrowAsync<AggregateException>();
    }
}
