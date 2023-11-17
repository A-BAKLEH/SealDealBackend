
using Core.Domain.ActionPlanAggregate;
using Core.Domain.AgencyAggregate;
using Core.Domain.AINurturingAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.NotificationAggregate;
using SharedKernel;

namespace Core.Domain.LeadAggregate;

public enum LeadStatus
{
    Hot, Active, Slow, Cold, Closed, Dead
}

public enum Language
{
    English, French
}

public class Lead : Entity<int>
{
    public int AgencyId { get; set; }
    public Agency Agency { get; set; }
    public bool verifyEmailAddress { get; set; } = false;
    public string LeadFirstName { get; set; }
    public string? LeadLastName { get; set; }
    public string? PhoneNumber { get; set; }
    //public string? Email { get; set; } moved to leadEmails
    public int? Budget { get; set; }
    public Language Language { get; set; } = Language.English;
    /// <summary>
    /// notifs that action plans running on this lead should handle because they can
    /// affect, such as leadStatusChange
    /// </summary>
    public EventType EventsForActionPlans { get; set; } = EventType.None;
    public bool HasActionPlanToStop { get; set; } = false;

    /// <summary>
    /// THIS IS ONLY VALID WHEN lead is assigned to a broker, not when unassigned
    /// </summary>
    public DateTime LastNotifsViewedAt { get; set; }
    public DateTime EntryDate { get; set; }
    public LeadSource source { get; set; }
    public LeadType leadType { get; set; }
    /// <summary>
    /// always onctains Creator Name and Id, try siteName/lead provider name
    /// </summary>
    public Dictionary<string, string> SourceDetails { get; set; } = new();
    public LeadStatus LeadStatus { get; set; } = LeadStatus.Hot;
    /// <summary>
    /// just a string for now, dont use areasOfInterest
    /// </summary>
    public string? Areas { get; set; }
    public Broker? Broker { get; set; }
    public Guid? BrokerId { get; set; }

    public List<Area>? AreasOfInterest { get; set; }
    /// <summary>
    /// lisitng that brought the lead
    /// </summary>
    public int? ListingId { get; set; }
    public Listing? Listing { get; set; }
    public Note? Note { get; set; }
    public List<LeadEmail>? LeadEmails { get; set; }
    public List<Tag>? Tags { get; set; }
    public List<ToDoTask> ToDoTasks { get; set; }
    public List<ActionPlanAssociation>? ActionPlanAssociations { get; set; }
    public List<AINurturing>? AINurturings { get; set; }

    /// <summary>
    /// when created by auto, message ID is in AppEvent of creation/assignation
    /// </summary>
    public List<AppEvent>? AppEvents { get; set; }
    public List<Notif>? Notifs { get; set; }
    public List<EmailEvent>? EmailEvents { get; set; }
}
public enum LeadSource
{
    manual, emailAuto, SmsAuto, unknown
}

public enum LeadType
{
    /// <summary>
    /// someone who wants to find a property to Buy
    /// </summary>
    Buyer,
    /// <summary>
    /// someone who wants to find a property to rent
    /// </summary>
    Renter,
    Unknown
}

