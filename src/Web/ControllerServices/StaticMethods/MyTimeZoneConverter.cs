namespace Web.ControllerServices.StaticMethods;

public static class MyTimeZoneConverter
{
    /// <summary>
    /// convert dateTime with given timeZone to UTC DateTimeOffset
    /// </summary>
    /// <param name="timeZoneInfo"></param>
    /// <param name="InputDateTime"></param>
    /// <returns></returns>
    public static DateTime ConvertToUTC(TimeZoneInfo timeZoneInfo, DateTime InputDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(InputDateTime, timeZoneInfo);
    }

    /// <summary>
    /// Convert from server-stored or 3rd-party obtained DateTime in UTC to local DateTime
    /// use .UtcDateTime to convert DateTimeOffset to DateTime
    /// </summary>
    /// <param name="timeZoneInfo"></param>
    /// <param name="InputDateTime"></param>
    /// <returns></returns>
    public static DateTime ConvertFromUTC(TimeZoneInfo timeZoneInfo, DateTime InputDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(InputDateTime, timeZoneInfo);
    }
}
