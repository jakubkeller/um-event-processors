using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Configuration;
using Models.Event;
using Services.Interface;

namespace Services.Implementation
{
    /// <summary>
    /// Writes RTPB transaction events to the RTPB reporting S3 bucket as a single JSON object each.
    /// Objects are partitioned using the existing yyyy/MM/dd/HH pattern and keyed by the generated MessageID.
    /// Downstream extraction/parsing is owned by the Data Lakehouse.
    /// </summary>
    public class RtpbReportingStorageService : IRtpbReportingStorageService
    {
        private const string ReportingBucketConfigKey = "RTPB_REPORTING_BUCKET_NAME";

        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        public RtpbReportingStorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName =
                configuration.GetValue<string>(ReportingBucketConfigKey)
                ?? throw new Exception($"Missing required configuration value {ReportingBucketConfigKey}");
        }

        public async Task<string> StoreRtpbTransactionAsync(RtpbReceiveEvent @event)
        {
            Logger.LogInformation("--- RtpbReportingStorageService.StoreRtpbTransactionAsync Start ---");

            if (string.IsNullOrWhiteSpace(@event.RequestMessageId))
                throw new ArgumentException("RtpbReceiveEvent.RequestMessageId is required to build the S3 object key.", nameof(@event));

            var objectKey = BuildObjectKey(@event.RequestMessageId);
            var body = JsonSerializer.Serialize(@event, _jsonSerializerOptions);

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                ContentType = "application/json",
                ContentBody = body,
            };

            await _s3Client.PutObjectAsync(request);

            Logger.LogInformation($"--- Stored RTPB transaction {@event.RequestMessageId} at s3://{_bucketName}/{objectKey} ---");

            return objectKey;
        }

        /// <summary>
        /// Builds the partitioned S3 object key: yyyy/MM/dd/HH/{MessageID}.json (UTC).
        /// </summary>
        private static string BuildObjectKey(string messageId)
        {
            var now = DateTime.UtcNow;
            return $"{now:yyyy}/{now:MM}/{now:dd}/{now:HH}/{messageId}.json";
        }
    }
}
