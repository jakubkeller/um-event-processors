using System;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using ServiceClients.Navitus.Document;
using Services.Mappers;
using Xunit;
using Microsoft.Graph.Models;

namespace UnitTests.Services
{
    public class SendEmailMapperTests
    {
        [Fact]
        public void MapToSendEmailRequest_SendEmail_ShouldMapCorrectly()
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

            // Act
            var result = sendEmail.MapToGraphMessage("test@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sendEmail.Subject, result.Subject);
            Assert.NotNull(result.Body);
            Assert.Equal(BodyType.Html, result.Body.ContentType);
            Assert.Equal(sendEmail.HtmlBody, result.Body.Content);
            Assert.Equal("test@test.com", result.From.EmailAddress?.Address);
            Assert.NotNull(result.ToRecipients);
            Assert.Single(result.ToRecipients);
            Assert.Equal("person@place.com", result.ToRecipients[0].EmailAddress?.Address);
            Assert.NotNull(result.CcRecipients);
            Assert.Single(result.CcRecipients);
            Assert.Equal("person@place.com", result.CcRecipients[0].EmailAddress?.Address);
            Assert.NotNull(result.BccRecipients);
            Assert.Empty(result.BccRecipients);
            Assert.NotNull(result.SingleValueExtendedProperties);
            Assert.Single(result.SingleValueExtendedProperties);
            var sensitivityProp = result.SingleValueExtendedProperties.First();
            Assert.Equal("Integer 0x0036", sensitivityProp.Id);
            Assert.Equal(sendEmail.Sensitivity.ToString(), sensitivityProp.Value);
        }
    }
}
