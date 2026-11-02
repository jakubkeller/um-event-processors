using Models.Event;
using Services.Mappers;
using Xunit;

namespace UnitTests.Services;

public class EmailMapperTests
{
    [Fact]
    public void MapToCreateEmailMetadataRequest_OutboundEmailReceived_ShouldMapCorrectly()
    {
        // Arrange
        var outboundEmailReceived = new OutboundEmailReceived
        {
            NavitusEpisodeId = 123,
            Subject = "test-subject",
            HtmlBody = "<p>Hello world</p>",
            ToEmailaddress = ["tester@test.com"],
            Sensitivity = 2,
            DocumentId = ["test-key1", "test-key2"]
        };

        // Act
        var result = EmailMapper.MapToCreateEmailMetadataRequest(outboundEmailReceived);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(outboundEmailReceived.NavitusEpisodeId, result.NavitusEpisodeId);
        Assert.Equal(outboundEmailReceived.Subject, result.Subject);
        Assert.Equal(outboundEmailReceived.HtmlBody, result.HtmlBody);
        Assert.Equal(outboundEmailReceived.Sensitivity, result.Sensitivity);
        Assert.Equal(outboundEmailReceived.ToEmailaddress, result.Recipients);
        Assert.Equal(outboundEmailReceived.DocumentId, result.Attachments);
        Assert.True((DateTimeOffset.UtcNow - result.ReceivedUTC).TotalSeconds < 1);
    }
}
