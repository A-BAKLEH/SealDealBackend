using Core.Domain.ActionPlanAggregate;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.Domain.TasksAggregate;
using SharedKernel;

namespace Core.Domain.BrokerAggregate;

public class Broker : Entity<Guid>
{
    public int AgencyId { get; set; }
    public Agency Agency { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Language Language { get; set; } = Language.English;

    ///<summary>
    /// in Windows Registry formt like: Eastern Standard Time
    /// use TZConvert.GetTimeZoneInfo() to get TimeZoneInfo
    /// </summary>
    public string? TimeZoneId { get; set; }
    public string? TempTimeZone { get; set; }
    public bool isSolo { get; set; } = false;
    public Boolean isAdmin { get; set; }
    public Boolean AccountActive { get; set; }
    public string? PhoneNumber { get; set; }
    public string LoginEmail { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// AppEvent triggers to Broker's active Action Plans OR for action plans
    /// that listen to notifs that ARE NOT RELATED DIRECTLY TO LEADS
    /// For lead-related notifs, there is a EventType field in the lead entity and
    /// HasActionPlanToStop
    /// </summary>
    public EventType ListenForActionPlans { get; set; } = EventType.None;

    /// <summary>
    /// mark emails as read after they are clicked in frontend
    /// </summary>
    public bool MarkEmailsRead { get; set; } = true;

    /// <summary>
    /// biggest AppEvent Id analyzed for this broker
    /// </summary>
    public int AppEventAnalyzerLastId { get; set; } = 0;

    /// <summary>
    /// just for admins (not solo brokers)
    /// </summary>
    public int LastUnassignedLeadIdAnalyzed { get; set; } = 0;

    /// <summary>
    /// last timestamp analyzed from emailEvents for this broker
    /// </summary>
    public DateTime EmailEventAnalyzerLastTimestamp { get; set; }
    public bool hasConnectedCalendar { get; set; } = false;
    public bool CalendarSyncEnabled { get; set; } = false;
    public bool SMSNotifsEnabled { get; set; } = false;
    public List<ConnectedEmail>? ConnectedEmails { get; set; }
    public List<Lead>? Leads { get; set; }
    public List<BrokerListingAssignment>? AssignedListings { get; set; }
    public List<Template>? Templates { get; set; }
    public List<ToDoTask>? Tasks { get; set; }
    public List<Tag>? BrokerTags { get; set; }
    public List<ActionPlan>? ActionPlans { get; set; }
    public List<RecurrentTaskBase>? RecurrentTasks { get; set; }
    public List<AppEvent>? AppEvents { get; set; }
    public List<Notif>? Notifs { get; set; }
    public List<EmailEvent>? EmailEvents { get; set; }
}
