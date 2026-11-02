using Models.Event;

namespace Services.Interface
{
    public interface IDocumentService
    {
        Task EnsureAuthenticated();
        Task<bool> ProcessOutboundMailReceivedEvent(OutboundMailReceived @event);
        Task<bool> ProcessOutboundFaxReceivedEvent(OutboundFaxReceived @event);
    }
}
