using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace UM.Infra.EventProcessors;

public class AWSCredentialHelper
{
    private static AWSCredentials? _awsCredentials = null;

    public static AWSCredentials? LoadSsoCredentials()
    {
        var chain = new CredentialProfileStoreChain();
        if (!chain.TryGetAWSCredentials(System.Environment.GetEnvironmentVariable("AWS_PROFILE") ?? "default", out var credentials))
        {
            Console.WriteLine("Unable to retrieve default credentials via SSO.");
            return null;
        }

        Console.WriteLine("Using credentials associated with SSO default profile.");
        return credentials;
    }

    public static AWSCredentials? LoadCredentialsFromEnv() => new EnvironmentVariablesAWSCredentials();

    public static AWSCredentials? TryLoadCredentials()
    {
        if (_awsCredentials != null)
            return _awsCredentials;

        _awsCredentials = LoadSsoCredentials() ?? LoadCredentialsFromEnv();
        return _awsCredentials;
    }
}
