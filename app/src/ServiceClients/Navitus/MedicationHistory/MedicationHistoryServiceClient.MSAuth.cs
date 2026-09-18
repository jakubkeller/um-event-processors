using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceClients.Navitus.MedicationHistory;

/// <summary>
/// Partial class extension for the NSwag-generated MedicationHistoryServiceClient.
/// Overrides auth to use MSAuth tokens (via IMSAuthTokenProvider) instead of Bearer tokens.
/// </summary>
public partial class MedicationHistoryServiceClient
{
    private readonly IMSAuthTokenProvider _msAuthTokenProvider;

    [ActivatorUtilitiesConstructor]
    public MedicationHistoryServiceClient(System.Net.Http.HttpClient httpClient, IMSAuthTokenProvider msAuthTokenProvider)
        : this(httpClient)
    {
        _msAuthTokenProvider = msAuthTokenProvider ?? throw new ArgumentNullException(nameof(msAuthTokenProvider));
    }

    public override async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
    {
        var msg = new HttpRequestMessage();
        var token = await _msAuthTokenProvider.GetTokenAsync(cancellationToken);
        msg.Headers.Authorization = new AuthenticationHeaderValue("MSAuth", token);
        return msg;
    }
}
