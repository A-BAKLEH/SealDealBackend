using SharedKernel;

namespace Core.Domain.BrokerAggregate.EmailConnection;
public class ConnectedEmail : EntityBase
{
    public Guid BrokerId { get; set; }

    /// <summary>
    /// in order of connection, 1 being the first
    /// </summary>
    public byte EmailNumber { get; set; }
    public Broker Broker { get; set; }

    /// <summary>
    /// only relevant for admins, if false then leads won't be automatically assigned to brokers
    /// </summary>
    public bool AssignLeadsAuto { get; set; } = true;
    /// <summary>
    /// primary key
    /// </summary>
    public string Email { get; set; }
    public bool isMSFT { get; set; }
    public bool isMailbox { get; set; } = true;
    public bool isCalendar { get; set; } = false;

    //microsoft-----------
    public bool hasAdminConsent { get; set; } //gmail sets this to true but doesnt use it, but necessary cuz its checked
    //to see if email has consent when creating action plan
    public string tenantId { get; set; }
    public Guid? GraphSubscriptionId { get; set; } //for msft webhook
    public DateTime? SubsExpiryDate { get; set; } //not really used except for documenting expiry date
    //-----------------------

    //SHARED
    /// <summary>
    /// for gmail, this is recurrent job that calls Watch every day.
    /// </summary>
    public string? SubsRenewalJobId { get; set; } //to get notifs, NOTHING TO DO WITH CALENDAR

    //-----calendar
    //public bool CalendarSyncEnabled { get; set; } = false; maybe add later

    //----------Gmail-----------
    public string? RefreshToken { get; set; }
    public string? AccessToken { get; set; }
    public string? TokenRefreshJobId { get; set; }
    public string? historyId { get; set; }

    //------------------generic email parsing properties------------------
    /// <summary>
    /// when true, sync will happen shortly 
    /// </summary>
    public bool SyncScheduled { get; set; } = false;
    public string? SyncJobId { get; set; }
    /// <summary>
    /// received property of last email fetched
    /// </summary>
    public DateTime? LastSync { get; set; }

    /// <summary>
    /// DateTime of first email connection
    /// </summary>
    public DateTime? FirstSync { get; set; }

    public int OpenAITokensUsed { get; set; } = 0;
}
