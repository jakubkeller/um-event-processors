using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.BatchProcessing;
using AWS.Lambda.Powertools.BatchProcessing.Sqs;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RtpbReceiveProcessor
{
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
        /// Function Handler which processes RTPBReceiveEvents and persists each transaction as a single
        /// JSON object to the RTPB reporting S3 bucket for downstream Data Lakehouse consumption.
        /// </summary>
        /// <param name="_sqsEvent">SQS Event which contains a batch of RTPBReceiveEvents to process</param>
        /// <param name="_context">Context object provided by AWS Lambda service with details about the invocation event</param>
        /// <returns></returns>
        [Logging(ClearState = true)]
        public async Task<BatchItemFailuresResponse> FunctionHandler(SQSEvent _sqsEvent, ILambdaContext _context)
        {
            var batchProcessor = _serviceProvider.GetRequiredService<ISqsBatchProcessor>();
            var recordHandler = _serviceProvider.GetRequiredService<ISqsRecordHandler>();

            var result = await batchProcessor.ProcessAsync(_sqsEvent, recordHandler);
            return result.BatchItemFailuresResponse;
        }
    }
}
