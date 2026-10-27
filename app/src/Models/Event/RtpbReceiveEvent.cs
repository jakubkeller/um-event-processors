namespace Models.Event
{
    /// <summary>
    /// Represents an RTPB (Real-Time Pharmacy Benefit) transaction received for reporting.
    /// Published to the UM event bus and consumed by the RtpbReceiveProcessor, which persists
    /// a single JSON object (this event) to the RTPB reporting S3 bucket for the Data Lakehouse.
    ///
    public class RtpbReceiveEvent
    {
        public string RequestMessageId { get; set; } = default!;

        public string? ResponseMessageId { get; set; }

        public DateTimeOffset RequestTimestamp { get; set; } = default!;

        public DateTimeOffset ResponseTimestamp { get; set; } = default!;

        public string Vendor { get; set; } = default!;

        public bool? Status { get; set; }

        public string RequestXml { get; set; } = default!;

        public string ResponseXml { get; set; } = default!;
    }
}
