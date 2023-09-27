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
    public bool hasAdminConsent { get; set; }
    public string tenantId { get; set; }
    public Guid? GraphSubscriptionId { get; set; }
    public DateTime? SubsExpiryDate { get; set; }
    /// <summary>
    /// for gmail, this is recurrent job that calls Watch every day
    /// </summary>
    public string? SubsRenewalJobId { get; set; }

    //-----calendar
    public bool HasCalendarPermissions { get; set; } = false;
    public bool CalendarSyncEnabled { get; set; } = false;

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
