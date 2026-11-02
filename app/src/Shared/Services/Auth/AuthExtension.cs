using AWS.Lambda.Powertools.Logging;

namespace Shared.Services.Auth
{
    public static class AuthExtension
    {
        public static async Task<string> AuthenticateInternallyNucleus(this IAuthProvider authProvider, params string[] scopes)
        {
            Logger.LogInformation("--- AuthenticateInternallyNucleus Start. ---");

            string bearerToken = await authProvider.GetTokenAsync(scopes);

            Logger.LogDebug("--- AuthenticateInternallyNucleus Completed. ---");

            return bearerToken;
        }
    }
}
