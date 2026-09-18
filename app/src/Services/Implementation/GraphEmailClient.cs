using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Extensions.Configuration;

public class GraphEmailClient : IGraphEmailClient
{
    private readonly GraphServiceClient _client;
    private readonly IConfiguration _configuration;

    public GraphEmailClient(GraphServiceClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(Message message, bool saveToSentItems)
    {
        var graphUser = _configuration.GetValue<string>("GRAPH_CLIENT_USER")
            ?? throw new Exception("Missing required configuration value GRAPH_CLIENT_USER");

        var body = new SendMailPostRequestBody
        {
            Message = message,
            SaveToSentItems = saveToSentItems
        };

        await _client.Users[graphUser].SendMail.PostAsync(body);
    }
}