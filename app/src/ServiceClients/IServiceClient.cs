namespace ServiceClients
{
    public interface IServiceClient : ICanAuthenticate
    {
        string BaseUrl { get; set; }
    }
}
