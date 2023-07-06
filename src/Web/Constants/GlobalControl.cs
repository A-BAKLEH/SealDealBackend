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
    public static List<string> LeadProviderEmails = new()
    {   "noreply@point2.com",
        "do-not-reply@centris.ca",
        "Lead@realtor.ca",
        "basharo9999@hotmail.com"
    };

    public static List<string> ProcessingIgnoreEmails = new()
    {
        "immocontact@telmatik.com","info@oaciq.com"
    };

    public static List<string> ProcessingIgnoreDomains = new()
    {
        "apciq.ca",
        "qpareb.ca",
        "crea.ca"
    };

    public static List<Guid> OurIds = new()
    {
        Guid.Parse("c8b0455a-876b-4804-a3f2-1d2bb103e910"),
        Guid.Parse("48e494e3-16da-4349-842a-36830dbec1bd")
    };

    public static TimeSpan EmailStartSyncingDelay = TimeSpan.FromSeconds(5);

    public static bool ProcessEmails = true;
    public static bool ProcessFailedEmailsParsing = true;
    public static bool LogOpenAIEmailParsingObjects = false;


    public static bool LogAllEmailsLengthsOpenAi = true;
}
