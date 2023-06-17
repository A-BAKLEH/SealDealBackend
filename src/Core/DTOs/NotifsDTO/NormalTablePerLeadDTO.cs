using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;

namespace Core.DTOs.NotifsDTO;
public class AppEventForNotifSelectedDTO
{
    public int Id { get; set; }
    public int LeadID { get; set; }
    public DateTime EventTimeStamp { get; set; }
    public EventType eventType { get; set; }
    public LeadStatus LeadStatus { get; set; }
    public Dictionary<string, string> Props { get; set; }
    public string LeadFirstName { get; set; }
    public string LeadLastName { get; set; }
    public string LeadPhone { get; set; }
    public string LeadEmail { get; set; }
}
public class LeadForNotifsDTO
{
    public Guid? brokerId { get; set; }
    public int LeadId { get; set; }
    public string LeadfirstName { get; set; }
    public string LeadLastName { get; set; }
    public string LeadPhone { get; set; }
    /// <summary>
    /// main email
    /// </summary>
    public string LeadEmail { get; set; }
    public string LeadStatus { get; set; }
    public DateTime LastTimeYouViewedLead { get; set; }
}
//actual sent DTOs

public class CompleteDashboardDTO

{
    public List<DashboardPerLeadDTO> LeadRelatedNotifs { get; set; }
    public List<AppEventsNonLeadDTO>? OtherNotifs { get; set; }
}
public class AppEventsNonLeadDTO
{
    public int AppEventID { get; set; }
    public string EventType { get; set; }
    public DateTime EventTimeStamp { get; set; }
    public Dictionary<string, string>? Kes { get; set; }
}
/// <summary>
/// just leave null lists that arent used
/// </summary>
public class DashboardPerLeadDTO
{
    public int LeadId { get; set; }
    public bool LeadUnAssigned { get; set; }
    public string LeadfirstName { get; set; }
    public string LeadLastName { get; set; }
    public string LeadPhone { get; set; }
    /// <summary>
    /// main email
    /// </summary>
    public string LeadEmail { get; set; }
    public string LeadStatus { get; set; }
    public DateTime? LastTimeYouViewedLead { get; set; }
    /// <summary>
    /// timestamp of most recent app Event or email event (whichever is more recent) belonging to this lead
    /// or null if no app events or email events
    /// </summary>
    public DateTime? MostRecentEventOrEmailTime { get; set; }
    public byte? HighestPriority { get; set; }
    public IEnumerable<NormalTableLeadAppEventDTO>? AppEvents { get; set; }
    public IEnumerable<NormalTableLeadEmailEventDTO>? EmailEvents { get; set; }
    public IEnumerable<PriorityTableLeadNotifDTO>? PriorityNotifs { get; set; }
}
public class NormalTableLeadAppEventDTO
{
    public bool Seen { get; set; }
    public int AppEventID { get; set; }
    public string EventType { get; set; }
    public DateTime EventTimeStamp { get; set; }
}
//unseen / unreplied-to emails
public class NormalTableLeadEmailEventDTO
{
    public string EmailId { get; set; }
    public DateTime Received { get; set; }
    public bool Seen { get; set; }
    public bool NeedsAction { get; set; }
    public bool RepliedTo { get; set; }
}

public class PriorityTableLeadNotifDTO
{
    public int NotifID { get; set; }
    public byte Priority { get; set; }
    public string EventType { get; set; }
    public string? EmailID { get; set; }
    public DateTime EventTimeStamp { get; set; }
}

