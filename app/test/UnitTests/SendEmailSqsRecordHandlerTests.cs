using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using SendEmailProcessor;
using Models.Event;
using NSubstitute;
using Services.Interface;
using Xunit;
using Services.Implementation;

namespace UnitTests
{
    public class SendEmailSqsRecordHandlerTests
    {
        private readonly ISendEmailService _mockSendEmailService;
        private readonly SendEmailSqsRecordHandler _handler;

        public SendEmailSqsRecordHandlerTests()
        {
            _mockSendEmailService = Substitute.For<ISendEmailService>();
            _handler = new SendEmailSqsRecordHandler(_mockSendEmailService);
        }

        [Fact]
        public async Task HandleAsync_WithSendEmailReceived_ShouldProcessCorrectly()
        {
            // Arrange
            var sendEmail = new SendEmail
            {
                NavitusEpisodeId = "123",
                Subject = "Subject",
                HtmlBody = "Body",
                Sensitivity = 0,
                ToEmailaddress = ["person@place.com"],
                CcEmailaddress = ["person@place.com"],
                BccEmailaddress = [],
                DocumentId = ["123", "456"]
            };

            var cloudWatchEvent = new CloudWatchEvent<JsonElement>
            {
                DetailType = nameof(SendEmail),
                Detail = JsonSerializer.SerializeToElement(sendEmail)
            };

            var sqsMessage = new SQSEvent.SQSMessage { Body = JsonSerializer.Serialize(cloudWatchEvent) };

            // Act
            var result = await _handler.HandleAsync(sqsMessage, CancellationToken.None);

            // Assert
            await _mockSendEmailService
                .Received(1)
                .ProcessSendEmailEvent(
                    Arg.Is<SendEmail>(e =>
                        e.NavitusEpisodeId == sendEmail.NavitusEpisodeId
                        && e.Subject == sendEmail.Subject
                        && e.HtmlBody == sendEmail.HtmlBody
                        && e.Sensitivity == sendEmail.Sensitivity
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
