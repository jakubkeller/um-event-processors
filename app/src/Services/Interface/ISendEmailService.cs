using Models.Event;

namespace Services.Interface;

public interface ISendEmailService
{
    Task EnsureAuthenticated();
    Task<bool> ProcessSendEmailEvent(SendEmail @event);
}
