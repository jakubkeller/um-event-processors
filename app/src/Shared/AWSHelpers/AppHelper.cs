namespace Shared.AWSHelpers
{
    public static class AppHelper
    {
        /// <summary>
        /// Checks if the app is running in a Lambda environment.
        /// </summary>
        /// <returns></returns>
        ///
        public static bool IsRunningInLambda()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
        }

        /// <summary>
        /// Get Current Region
        /// </summary>
        /// <returns></returns>
        public static string GetExecutionRegion()
        {
            return Environment.GetEnvironmentVariable("AWS_REGION") ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "us-east-2";
        }
    }
}
