
using Clean.Architecture.Core.Domain.ActionPlanAggregate;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.Core.Domain.TasksAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class Broker : Entity<Guid>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public Boolean isAdmin { get; set; }
  public Boolean AccountActive { get; set; }
  public string? PhoneNumber { get; set; }
  public string LoginEmail { get; set; }
  public string FirstConnectedEmail { get; set; } = "";
  public DateTime Created { get; set; } = DateTime.UtcNow;
  /// <summary>
  /// Notif types that can act as trigger/stoppage/etc in Broker's active Action Plans
  /// </summary>
  public NotifType? NotifsForActionPlans { get; set; }
  public List<Lead>? Leads { get; set; }
  public List<BrokerListingAssignment>? AssignedListings { get; set; }
  public List<Template>? Templates { get; set; }
  public List<ToDoTask>? Tasks { get; set; }
  public List<Tag>? BrokerTags { get; set; }
  public List<ActionPlan>? ActionPlans { get; set; }
  public List<RecurrentTaskBase>? RecurrentTasks { get; set; }
}
