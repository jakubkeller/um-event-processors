using Microsoft.Graph.Models;

public interface IGraphEmailClient
{
    Task SendEmailAsync(Message message, bool saveToSentItems);
}