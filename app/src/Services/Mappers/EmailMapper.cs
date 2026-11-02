using Models.Event;
using ServiceClients.Navitus;

namespace Services.Mappers;

public static class EmailMapper
{
    /// <summary>
    /// Maps email received event to a CreateEmailMetadataRequest
    /// </summary>
    /// <param name="@event"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static CreateEmailMetadataRequest MapToCreateEmailMetadataRequest(this OutboundEmailReceived @event)
    {
        var result = new CreateEmailMetadataRequest
        {
            NavitusEpisodeId = @event.NavitusEpisodeId,
            ExternalEpisodeId = @event.ExternalEpisodeId,
            Subject = @event.Subject,
            HtmlBody = @event.HtmlBody,
            Sensitivity = @event.Sensitivity,
            Recipients = [.. @event.ToEmailaddress],
            Cc = @event?.CcEmailaddress is null ? null : [.. @event.CcEmailaddress],
            Bcc = @event?.BccEmailaddress is null ? null : [.. @event.BccEmailaddress],
            Attachments = @event?.DocumentId is null ? null : [.. @event.DocumentId],
            ReceivedUTC = DateTimeOffset.UtcNow,
        };

        return result;
    }
}
