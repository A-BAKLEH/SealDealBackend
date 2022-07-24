
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.BrokerAggregate;

public class ToDoTask : Entity<int>
{ 

  //public int? AgencyId { get; set; }
  //public Agency? Agency { get; set; }
  public string TaskText { get; set; }
  public DateTime TaskDueDate { get; set; }

  public Guid BrokerId { get; set; }
  public Broker Broker { get; set; }
  public int? LeadId { get; set; }
  public Lead? Lead { get; set; }
}

