using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Interface;
using Shared.Extensions;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TcnPollingProcessor;

public class Function
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// This constructor is provided as an alternative entrypoint
    /// Useful for unit testing.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public Function(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Function class entrypoint invoked by AWS Lambda.
    /// Anything in this function will run on cold-start.
    /// </summary>
    public Function()
    {
        Services.Init();
        _serviceProvider = Services.Provider;
    }

    /// <summary>
    /// Function Handler which polls pending TCN call details and updates results from TCN.
    /// </summary>
    /// <param name="_scheduledEvent">CloudWatch scheduled event that triggers this processor every 15 minutes</param>
    /// <param name="_context">Context object provided by AWS Lambda service with details about the invocation event</param>
    [Logging(ClearState = true)]
    public async Task FunctionHandler(CloudWatchEvent<object> _scheduledEvent, ILambdaContext _context)
    {
        Logger.LogInformation("--- TcnPollingProcessor FunctionHandler Invoked. ---");

        var tcnPollingService = _serviceProvider.GetRequiredService<ITcnPollingService>();
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();

        await tcnPollingService.EnsureAuthenticated();

        var batchSize = configuration.GetValue<int>("TCN_POLL_BATCH_SIZE", 100);
        var cancellationToken = _context.RemainingTime.ToCancellationToken();

        await tcnPollingService.PollPendingCallDetailsAsync(batchSize, cancellationToken);

        Logger.LogInformation("--- TcnPollingProcessor FunctionHandler Complete. ---");
    }
}
