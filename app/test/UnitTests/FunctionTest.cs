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
using TemplatedNoteProcessor;
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
        mockServices.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessorSqsRecordHandler>();

        var mockProvider = mockServices.BuildServiceProvider();

        List<SQSEvent.SQSMessage> SqsMessages = [];

        var request = new SQSEvent { Records = SqsMessages };
        var context = new TestLambdaContext();
        var function = new Function(mockProvider);

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
        mockServices.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessorSqsRecordHandler>();

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
        var function = new Function(mockProvider);

        var result = await function.FunctionHandler(request, context);

        result.BatchItemFailures.Count.Should().Be(0);
    }

    [Fact]
    public async Task MultipleValidRecord_Should_ProcessSuccessfully()
    {
        var _mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();

        var mockServices = new ServiceCollection();

        mockServices.AddSingleton((services) => _mockTemplatedNoteService);
        mockServices.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessorSqsRecordHandler>();

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
        var function = new Function(mockProvider);

        var result = await function.FunctionHandler(request, context);

        result.BatchItemFailures.Count.Should().Be(0);
    }

    [Fact]
    public async Task MultipleValidRecordWithOneInvalidRecord_Should_ProcessBatchSuccessfullyAndReportOneFailure()
    {
        var _mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();

        var mockServices = new ServiceCollection();

        mockServices.AddSingleton((services) => _mockTemplatedNoteService);
        mockServices.AddSingleton<ISqsBatchProcessor, SqsBatchProcessorWithLogging>();
        mockServices.AddSingleton<ISqsRecordHandler, TemplatedNoteProcessorSqsRecordHandler>();

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
        var function = new Function(mockProvider);

        var result = await function.FunctionHandler(request, context);

        result.BatchItemFailures.Count.Should().Be(1);
        result.BatchItemFailures[0].ItemIdentifier.Should().Be(invalidEventId);
    }
}
