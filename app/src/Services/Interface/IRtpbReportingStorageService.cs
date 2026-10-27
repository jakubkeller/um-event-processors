using Models.Event;

namespace Services.Interface
{
    public interface IRtpbReportingStorageService
    {
        /// <summary>
        /// Persists an RTPB transaction as a single JSON object to the RTPB reporting S3 bucket.
        /// The object key is partitioned yyyy/MM/dd/HH and named using the event's generated MessageID.
        /// </summary>
        /// <param name="event">The RTPB transaction to store.</param>
        /// <returns>The S3 object key the event was written to.</returns>
        Task<string> StoreRtpbTransactionAsync(RtpbReceiveEvent @event);
    }
}
