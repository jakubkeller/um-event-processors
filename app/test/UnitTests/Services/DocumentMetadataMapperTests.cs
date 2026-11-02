using System;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using ServiceClients.Navitus.Document;
using Services.Mappers;
using Xunit;

namespace UnitTests.Services
{
    public class DocumentMetadataMapperTests
    {
        [Fact]
        public void MapToCreateDocumentMetadataRequest_OutboundMailReceived_ShouldMapCorrectly()
        {
            // Arrange
            var outboundMailReceived = new OutboundMailReceived
            {
                S3ObjectKey = "test-mail-key",
                ExternalEpisodeId = "ext-123",
                NavitusEpisodeId = "456",
                RecipientName = "John Doe",
                TransactionId = "trans-789"
            };

            // Act
            var result = DocumentMetadataMapper.MapToCreateDocumentMetadataRequest(outboundMailReceived);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(outboundMailReceived.S3ObjectKey, result.DocumentId);
            Assert.Equal(DocumentType.Mail, result.DocumentType);
            Assert.Equal(outboundMailReceived.ExternalEpisodeId, result.ExternalEpisodeId);
            Assert.Equal(long.Parse(outboundMailReceived.NavitusEpisodeId), result.NavitusEpisodeId);
            Assert.Equal(outboundMailReceived.RecipientName, result.RecipientName);
            Assert.Equal(outboundMailReceived.TransactionId, result.TransactionId);
            Assert.True((DateTimeOffset.UtcNow - result.ReceivedUTC).TotalSeconds < 1);
        }

        [Fact]
        public void MapToCreateDocumentMetadataRequest_OutboundFaxReceived_ShouldMapCorrectly()
        {
            // Arrange
            var outboundFaxReceived = new OutboundFaxReceived
            {
                S3ObjectKey = "test-fax-key",
                ExternalEpisodeId = "ext-456",
                NavitusEpisodeId = "789",
                RecipientName = "Jane Smith",
                TransactionId = "trans-012"
            };

            // Act
            var result = DocumentMetadataMapper.MapToCreateDocumentMetadataRequest(outboundFaxReceived);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(outboundFaxReceived.S3ObjectKey, result.DocumentId);
            Assert.Equal(DocumentType.Fax, result.DocumentType);
            Assert.Equal(outboundFaxReceived.ExternalEpisodeId, result.ExternalEpisodeId);
            Assert.Equal(long.Parse(outboundFaxReceived.NavitusEpisodeId), result.NavitusEpisodeId);
            Assert.Equal(outboundFaxReceived.RecipientName, result.RecipientName);
            Assert.Equal(outboundFaxReceived.TransactionId, result.TransactionId);
            Assert.True((DateTimeOffset.UtcNow - result.ReceivedUTC).TotalSeconds < 1);
        }
    }
}
