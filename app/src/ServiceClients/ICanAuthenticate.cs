namespace ServiceClients
{
    public interface ICanAuthenticate
    {
        string BearerToken { get; set; }
        Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken);
    }
}
