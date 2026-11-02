namespace Services.Interface;

public interface ITcnPollingService
{
    Task EnsureAuthenticated();
    Task PollPendingCallDetailsAsync(int batchSize, CancellationToken cancellationToken);
}
