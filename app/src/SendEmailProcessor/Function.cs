using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.DependencyInjection;
using Services.Interface;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SendEmailProcessor;

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
    /// Function Handler which processes Outbound emails
    /// </summary>
    /// <param name="_sqsEvent">SQS Event which contains a batch of UM Events to process by the Send Email Processor</param>
    /// <param name="_context">Context object provided by AWS Lambda service with details about the invocation event</param>
    /// <returns></returns>
    [Logging(ClearState = true)]
    public async Task<BatchItemFailuresResponse> FunctionHandler(SQSEvent _sqsEvent, ILambdaContext _context)
    {
        var batchProcessor = _serviceProvider.GetRequiredService<ISqsBatchProcessor>();
        var recordHandler = _serviceProvider.GetRequiredService<ISqsRecordHandler>();
        var sendEmailService = _serviceProvider.GetRequiredService<ISendEmailService>();

        // Ensuring the service has a fresh authorization token to invoke UM API if necessary
        // This removes the need to retrieve a token for each message in the batch.
        await sendEmailService.EnsureAuthenticated();

        var result = await batchProcessor.ProcessAsync(_sqsEvent, recordHandler);
        return result.BatchItemFailuresResponse;
    }
}
