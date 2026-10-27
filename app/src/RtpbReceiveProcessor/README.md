# UM RTPB Receive Processor

**Purpose:** Event-driven processor (PBI 292733) that consumes `RTPBReceiveEvent`s from the UM
event bus (EventBridge → SQS) and persists each RTPB transaction as a single JSON object to the
RTPB reporting S3 bucket. The Data Lakehouse (DLH) reads from this bucket and loads the data into
the analytics database; downstream extraction/parsing is owned by DLH.

## Object layout
- One JSON object per transaction containing the full `RTPBReceiveEvent`
  (`RequestXml`, `ResponseXml`, `RequestMessageId`, `ResponseMessageId`, `RequestTimestamp`,
  `ResponseTimestamp`, `Vendor`, `Status`).
- Object key: `yyyy/MM/dd/HH/{RequestMessageId}.json` (UTC partition prefix).

## Configuration
- `RTPB_REPORTING_BUCKET_NAME` — target S3 bucket name (set via Lambda environment variable in infra).
