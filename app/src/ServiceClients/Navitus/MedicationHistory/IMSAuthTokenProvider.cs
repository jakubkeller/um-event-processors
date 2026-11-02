namespace ServiceClients.Navitus.MedicationHistory;

/// <summary>
/// Provides MSAuth tokens for Gen 1 on-prem API authentication.
/// Flow: Entra client_credentials → local MSAuth token build.
/// </summary>
public interface IMSAuthTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
