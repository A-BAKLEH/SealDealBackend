
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
  /// <summary>
  /// in IANA formt like: America/Toronto
  /// use TZConvert.GetTimeZoneInfo() to get TimeZoneInfo
  /// </summary>
  //public string IanaTimeZone { get; set; }

  ///<summary>
  /// in Windows Registry formt like: Eastern Standard Time
  /// use TZConvert.GetTimeZoneInfo() to get TimeZoneInfo
  /// </summary>
  public string? TimeZoneId { get; set; }
  public string? TempTimeZone { get; set; }
  public Boolean isAdmin { get; set; }
  public Boolean AccountActive { get; set; }
  public string? PhoneNumber { get; set; }
  public string LoginEmail { get; set; }
  public DateTimeOffset Created { get; set;}
  /// <summary>
  /// Notif types that can act as trigger/stoppage/etc in Broker's active Action Plans
  /// </summary>
  public NotifType? NotifsForActionPlans { get; set; }
  public List<ConnectedEmail>? ConnectedEmails { get; set; }
  public List<Lead>? Leads { get; set; }
  public List<BrokerListingAssignment>? AssignedListings { get; set; }
  public List<Template>? Templates { get; set; }
  public List<ToDoTask>? Tasks { get; set; }
  public List<Tag>? BrokerTags { get; set; }
  public List<ActionPlan>? ActionPlans { get; set; }
  public List<RecurrentTaskBase>? RecurrentTasks { get; set; }
}
