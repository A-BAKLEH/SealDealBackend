

using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.TasksAggregate;
public abstract class HangfireTaskBase : Entity<int>
{
  public int? BrokerId { get; set; }
  public Broker Broker { get; set; }

}
/// <summary>
/// will start empty with NoTask then as a task is updated it will be switched between Scheduled and Done
/// </summary>
public enum TaskStatus { NoTask, Scheduled,Done}
