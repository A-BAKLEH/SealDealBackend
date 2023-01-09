
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.Domain.BrokerAggregate;

public class ToDoTask : Entity<int>
{ 
  /// <summary>
  /// description
  /// </summary>
  public string? Description { get; set; }
  public string TaskName { get; set; }
  public DateTimeOffset TaskDueDate { get; set; }

  public Guid BrokerId { get; set; }
  public Broker Broker { get; set; }
  public int? LeadId { get; set; }
  public Lead? Lead { get; set; }
}

