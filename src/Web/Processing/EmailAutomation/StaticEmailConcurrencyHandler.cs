using System.Collections.Concurrent;

namespace Web.Processing.EmailAutomation;

public static class StaticEmailConcurrencyHandler
{
    /// <summary>
    /// guid is the msft subscription id who's email parsing is being scheduled.
    /// When subsID is a key in dictionary it means that the email parsing is scheduled/running.
    /// </summary>
    public static ConcurrentDictionary<Guid, bool> EmailParsingdictMSFT =
        new ConcurrentDictionary<Guid, bool>();


    /// <summary>
    /// guid is the msft subscription id who's email parsing is being scheduled.
    /// When subsID is a key in dictionary it means that the email parsing is scheduled/running.
    /// </summary>
    public static ConcurrentDictionary<string, bool> EmailParsingdictGMAIL =
        new ConcurrentDictionary<string, bool>();

}
