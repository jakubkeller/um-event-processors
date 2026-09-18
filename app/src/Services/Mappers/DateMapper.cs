namespace Services.Mappers;

/// <summary>
/// Utility Class to convert DateTimeOffsets into NCRx DateTimes.
/// Note: Most events processed by this application will be DateTimeOffsets set to UTC.
/// </summary>
public static class DateMapper
{
    /// <summary>
    /// Null Safe NCRxDateTime Conversion.
    /// </summary>
    /// <param name="dateInput"></param>
    /// <returns></returns>
    public static string? ToNCRxDateTime(this DateTimeOffset? dateInput)
    {
        if (!dateInput.HasValue)
        {
            return string.Empty;
        }

        return ToNCRxDateTime(dateInput.Value);
    }

    /// <summary>
    /// Converts to CST which is what NCRx uses
    /// Note: The Central Time Zone subtracts six hours from UTC during standard time (UTC−6) and five hours during daylight saving time (UTC−5).
    /// </summary>
    /// <param name="dateInput"></param>
    /// <returns></returns>
    public static string ToNCRxDateTime(this DateTimeOffset dateInput)
    {
        var centralDate = TimeZoneInfo.ConvertTime(dateInput, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago")).DateTime;
        return centralDate.ToString() + " CT";
    }
}
