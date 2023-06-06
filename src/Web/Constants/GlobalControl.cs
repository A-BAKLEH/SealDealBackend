namespace Web.Constants;

/// <summary>
/// variables that control app functioning after app start
/// </summary>
public static class GlobalControl
{
    //------------testing----------------
    //centris
    //Point2 Homes
    //realtor
    public static List<string> LeadProviderEmails = new() { "noreply@point2.com",
        "do-not-reply@centris.ca",
        "Lead@realtor.ca",
        //"basharEskandar@hotmail.com",
        "basharo9999@hotmail.com"};

    public static TimeSpan EmailStartSyncingDelay = TimeSpan.FromSeconds(5);
}
