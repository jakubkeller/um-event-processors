using Models.Event;

namespace Services.Interface
{
    public interface IEmailService
    {
        Task EnsureAuthenticated();
        Task<bool> ProcessOutboundEmailReceivedEvent(OutboundEmailReceived @event);
    }
}
