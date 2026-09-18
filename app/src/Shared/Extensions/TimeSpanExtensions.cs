namespace Shared.Extensions;

public static class TimeSpanExtensions
{
    public static CancellationToken ToCancellationToken(this TimeSpan remainingTime)
    {
        var cts = new CancellationTokenSource(remainingTime);
        return cts.Token;
    }
}
