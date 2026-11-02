using Models.Event;

namespace Services.Interface;

public interface ITcnOralNotificationService
{
    Task EnsureAuthenticated();
    Task ScheduleBroadcastAsync(TcnInformationEvent @event, CancellationToken cancellationToken);
}
