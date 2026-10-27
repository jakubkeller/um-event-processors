using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Models.Event;
using NSubstitute;
using Services.Implementation;
using Xunit;

namespace UnitTests.Services
{
    public class RtpbReportingStorageServiceTests
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private const string BucketName = "test-rtpb-bucket";

        public RtpbReportingStorageServiceTests()
        {
            _s3Client = Substitute.For<IAmazonS3>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RTPB_REPORTING_BUCKET_NAME"] = BucketName
                })
                .Build();

            _s3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
                .Returns(new PutObjectResponse());
        }

        [Fact]
        public void Constructor_MissingBucketConfig_ThrowsException()
        {
            var emptyConfig = new ConfigurationBuilder().Build();

            var act = () => new RtpbReportingStorageService(_s3Client, emptyConfig);

            act.Should().Throw<Exception>().WithMessage("*RTPB_REPORTING_BUCKET_NAME*");
        }

        [Fact]
        public async Task StoreRtpbTransactionAsync_ValidEvent_CallsPutObjectWithCorrectBucketAndContentType()
        {
            var service = new RtpbReportingStorageService(_s3Client, _configuration);
            var @event = BuildEvent("test-message-id");

            await service.StoreRtpbTransactionAsync(@event);

            await _s3Client.Received(1).PutObjectAsync(
                Arg.Is<PutObjectRequest>(r =>
                    r.BucketName == BucketName &&
                    r.ContentType == "application/json"));
        }

        [Fact]
        public async Task StoreRtpbTransactionAsync_ValidEvent_ReturnsKeyContainingMessageId()
        {
            var service = new RtpbReportingStorageService(_s3Client, _configuration);
            var requestMessageId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var @event = BuildEvent(requestMessageId);

            var key = await service.StoreRtpbTransactionAsync(@event);

            key.Should().EndWith($"/{requestMessageId}.json");
        }

        [Fact]
        public async Task StoreRtpbTransactionAsync_ValidEvent_KeyFollowsPartitionPattern()
        {
            var service = new RtpbReportingStorageService(_s3Client, _configuration);
            var @event = BuildEvent("some-id");
            var before = DateTime.UtcNow;

            var key = await service.StoreRtpbTransactionAsync(@event);

            // key format: yyyy/MM/dd/HH/{messageId}.json
            var parts = key.Split('/');
            parts.Should().HaveCount(5);
            int.Parse(parts[0]).Should().BeInRange(before.Year, before.Year + 1);
            int.Parse(parts[1]).Should().BeInRange(1, 12);
            int.Parse(parts[2]).Should().BeInRange(1, 31);
            int.Parse(parts[3]).Should().BeInRange(0, 23);
        }

        [Fact]
        public async Task StoreRtpbTransactionAsync_ValidEvent_BodyContainsSerializedEvent()
        {
            var service = new RtpbReportingStorageService(_s3Client, _configuration);
            var @event = BuildEvent("msg-1", requestXml: "request-payload", responseXml: "response-payload");

            await service.StoreRtpbTransactionAsync(@event);

            var call = _s3Client.ReceivedCalls()
                .Single(c => c.GetMethodInfo().Name == nameof(IAmazonS3.PutObjectAsync));
            var request = (PutObjectRequest)call.GetArguments()[0]!;
            request.ContentBody.Should().Contain("msg-1");
            request.ContentBody.Should().Contain("request-payload");
            request.ContentBody.Should().Contain("response-payload");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task StoreRtpbTransactionAsync_EmptyOrNullMessageId_ThrowsArgumentException(string? requestMessageId)
        {
            var service = new RtpbReportingStorageService(_s3Client, _configuration);
            var @event = BuildEvent(requestMessageId!);

            var act = async () => await service.StoreRtpbTransactionAsync(@event);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*RequestMessageId*");
        }

        [Fact]
        public async Task StoreRtpbTransactionAsync_S3Throws_PropagatesException()
        {
            _s3Client.PutObjectAsync(Arg.Any<PutObjectRequest>())
                .Returns<PutObjectResponse>(_ => throw new AmazonS3Exception("S3 error"));

            var service = new RtpbReportingStorageService(_s3Client, _configuration);

            var act = async () => await service.StoreRtpbTransactionAsync(BuildEvent("id-1"));

            await act.Should().ThrowAsync<AmazonS3Exception>();
        }

        private static RtpbReceiveEvent BuildEvent(string requestMessageId, string requestXml = "<req/>", string responseXml = "<res/>") =>
            new()
            {
                RequestMessageId = requestMessageId,
                RequestXml = requestXml,
                ResponseXml = responseXml,
                RequestTimestamp = DateTimeOffset.UtcNow,
                ResponseTimestamp = DateTimeOffset.UtcNow,
                Vendor = "TestVendor",
            };
    }
}
