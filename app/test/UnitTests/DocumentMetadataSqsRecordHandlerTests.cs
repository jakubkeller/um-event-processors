using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using DocumentMetadataProcessor;
using Models.Event;
using NSubstitute;
using Services.Interface;
using Xunit;

namespace UnitTests
{
    public class DocumentMetadataSqsRecordHandlerTests
    {
        private readonly IDocumentService _mockDocumentService;
        private readonly DocumentMetadataSqsRecordHandler _handler;

        public DocumentMetadataSqsRecordHandlerTests()
        {
            _mockDocumentService = Substitute.For<IDocumentService>();
            _handler = new DocumentMetadataSqsRecordHandler(_mockDocumentService);
        }

        [Fact]
        public async Task HandleAsync_WithOutboundMailReceived_ShouldProcessCorrectly()
        {
            // Arrange
            var outboundMailReceived = new OutboundMailReceived
            {
                NavitusEpisodeId = "123",
                TransactionId = "456",
                S3ObjectKey = "test-key"
            };

            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = nameof(OutboundMailReceived),
                Detail = JsonSerializer.SerializeToElement(new { payload = outboundMailReceived })
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            // Act
            var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

            // Assert
            await _mockDocumentService
                .Received(1)
                .ProcessOutboundMailReceivedEvent(
                    Arg.Is<OutboundMailReceived>(e =>
                        e.NavitusEpisodeId == outboundMailReceived.NavitusEpisodeId
                        && e.TransactionId == outboundMailReceived.TransactionId
                        && e.S3ObjectKey == outboundMailReceived.S3ObjectKey
                    )
                );
            Assert.Equal(RecordHandlerResult.None, result);
        }

        [Fact]
        public async Task HandleAsync_WithOutboundFaxReceived_ShouldProcessCorrectly()
        {
            // Arrange
            var outboundFaxReceived = new OutboundFaxReceived
            {
                NavitusEpisodeId = "789",
                TransactionId = "101",
                S3ObjectKey = "test-fax-key"
            };

            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = nameof(OutboundFaxReceived),
                Detail = JsonSerializer.SerializeToElement(new { payload = outboundFaxReceived })
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            // Act
            var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

            // Assert
            await _mockDocumentService
                .Received(1)
                .ProcessOutboundFaxReceivedEvent(
                    Arg.Is<OutboundFaxReceived>(e =>
                        e.NavitusEpisodeId == outboundFaxReceived.NavitusEpisodeId
                        && e.TransactionId == outboundFaxReceived.TransactionId
                        && e.S3ObjectKey == outboundFaxReceived.S3ObjectKey
                    )
                );
            Assert.Equal(RecordHandlerResult.None, result);
        }

        [Fact]
        public async Task HandleAsync_WithInvalidEvent_ShouldThrowException()
        {
            // Arrange
            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = "InvalidEventType",
                Detail = JsonSerializer.SerializeToElement(new { payload = new { } })
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(() => _handler.HandleAsync(sqsMessage, CancellationToken.None));
        }
    }
}
