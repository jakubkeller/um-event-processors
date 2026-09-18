namespace ServiceClients.Navitus.MedicationHistory;

/// <summary>
/// Configuration options for the Gen 1 Medication History Service (RxHistory) client.
/// Populated from environment variables or SSM parameters prefixed with "MedicationHistory:".
/// </summary>
public class MedicationHistoryClientOptions
{
    public string? BaseUrl { get; set; }
    public string? EntraTokenUrl { get; set; }
    public string? EntraClientId { get; set; }
    public string? EntraClientSecret { get; set; }
    public string? EntraScope { get; set; }
    public string MsAuthEnv { get; set; } = "Test";
}
