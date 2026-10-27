using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using Models.Event;
using NSubstitute;
using RtpbReceiveProcessor;
using Services.Interface;
using Xunit;

namespace UnitTests
{
    public class RtpbReceiveSqsRecordHandlerTests
    {
        private readonly IRtpbReportingStorageService _mockStorageService;
        private readonly RtpbReceiveSqsRecordHandler _handler;

        public RtpbReceiveSqsRecordHandlerTests()
        {
            _mockStorageService = Substitute.For<IRtpbReportingStorageService>();
            _handler = new RtpbReceiveSqsRecordHandler(_mockStorageService);
        }

        [Fact]
        public async Task HandleAsync_WithRtpbReceiveEvent_ShouldStoreTransaction()
        {
            // Arrange
            var rtpbReceiveEvent = new RtpbReceiveEvent
            {
                RequestMessageId = "11111111-2222-3333-4444-555555555555",
                RequestXml = "<Request>request-xml</Request>",
                ResponseXml = "<Response>response-xml</Response>",
                RequestTimestamp = DateTimeOffset.UtcNow,
                ResponseTimestamp = DateTimeOffset.UtcNow,
                Vendor = "TestVendor",
            };

            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = nameof(RtpbReceiveEvent),
                Detail = JsonSerializer.SerializeToElement(rtpbReceiveEvent)
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            // Act
            var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

            // Assert
            await _mockStorageService
                .Received(1)
                .StoreRtpbTransactionAsync(
                    Arg.Is<RtpbReceiveEvent>(e =>
                        e.RequestMessageId == rtpbReceiveEvent.RequestMessageId
                        && e.RequestXml == rtpbReceiveEvent.RequestXml
                        && e.ResponseXml == rtpbReceiveEvent.ResponseXml
                        && e.Vendor == rtpbReceiveEvent.Vendor
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

        [Fact]
        public async Task HandleAsync_WithNullBody_ShouldThrowException()
        {
            var sqsMessage = new SQSEvent.SQSMessage { Body = "null" };

            await Assert.ThrowsAsync<Exception>(() => _handler.HandleAsync(sqsMessage, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_WithMalformedJson_ShouldThrowException()
        {
            var sqsMessage = new SQSEvent.SQSMessage { Body = "not-json" };

            await Assert.ThrowsAsync<JsonException>(() => _handler.HandleAsync(sqsMessage, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_WithNullDetail_ShouldThrowException()
        {
            // a JSON null detail deserializes to a null RtpbReceiveEvent
            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = nameof(RtpbReceiveEvent),
                Detail = JsonSerializer.SerializeToElement<object?>(null)
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            await Assert.ThrowsAsync<Exception>(() => _handler.HandleAsync(sqsMessage, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_WithRtpbReceiveEvent_ShouldNotCallStorageServiceMoreThanOnce()
        {
            var rtpbReceiveEvent = new RtpbReceiveEvent
            {
                RequestMessageId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                RequestXml = "<req/>",
                ResponseXml = "<res/>",
                RequestTimestamp = DateTimeOffset.UtcNow,
                ResponseTimestamp = DateTimeOffset.UtcNow,
                Vendor = "TestVendor",
            };

            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = nameof(RtpbReceiveEvent),
                Detail = JsonSerializer.SerializeToElement(rtpbReceiveEvent)
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            await _handler.HandleAsync(sqsMessage, CancellationToken.None);

            await _mockStorageService.Received(1).StoreRtpbTransactionAsync(Arg.Any<RtpbReceiveEvent>());
        }
    }
}
