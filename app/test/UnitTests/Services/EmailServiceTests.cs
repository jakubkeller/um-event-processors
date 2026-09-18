using Microsoft.Extensions.Configuration;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using Services.Implementation;
using Shared.Authorization;
using Shared.Services.Auth;
using Xunit;

namespace UnitTests.Services;

public class EmailServiceTests
{
    private readonly IAuthProvider _authProvider;
    private readonly IUMServiceClient _umServiceClient;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _authProvider = Substitute.For<IAuthProvider>();
        _umServiceClient = Substitute.For<IUMServiceClient>();
        _configuration = Substitute.For<IConfiguration>();

        _configuration.GetValue<string>("NavitusUtilizationManagementBaseURL").Returns("http://test-url.com");

        _emailService = new EmailService(_authProvider, _umServiceClient, _configuration);
    }

    [Fact]
    public async Task EnsureAuthenticated_ShouldSetBearerToken()
    {
        // Arrange
        string expectedToken = "test-token";
        _authProvider.AuthenticateInternallyNucleus(Arg.Any<string[]>()).Returns(expectedToken);

        // Act
        await _emailService.EnsureAuthenticated();

        // Assert
        await _authProvider
            .Received(1)
            .AuthenticateInternallyNucleus(
                Arg.Is<string[]>(scopes => scopes[0] == Scopes.Client.NucleusNavitus && scopes[1] == Scopes.PriorAuthEpisode.Write)
            );
        Assert.Equal(expectedToken, _umServiceClient.BearerToken);
    }

    [Fact]
    public async Task ProcessOutboundEmailReceivedEvent_ShouldCallCreateEmailMetadata()
    {
        // Arrange
        var outboundEmailReceived = new OutboundEmailReceived
        {
            NavitusEpisodeId = 123,
            Subject = "test-subject",
            HtmlBody = "<p>Hello world</p>",
            ToEmailaddress = ["tester@test.com"],
            DocumentId = ["test-key"]
        };

        // Act
        var result = await _emailService.ProcessOutboundEmailReceivedEvent(outboundEmailReceived);

        // Assert
        await _umServiceClient.Received(1).Episode_CreateEmailMetadataAsync(Arg.Any<CreateEmailMetadataRequest>());
        Assert.True(result);
    }
}
