using Core.Domain.BrokerAggregate;
using SharedKernel;

namespace Core.Domain.TasksAggregate;
public abstract class RecurrentTaskBase : Entity<int>
{
  public Guid? BrokerId { get; set; }
  public Broker Broker { get; set; }
  public string? HangfireTaskId { get; set; }
  public DateTimeOffset? NextScheduledTime { get; set;}
  public HangfireTaskStatus taskStatus { get; set; } = HangfireTaskStatus.NoTask;

  /// <summary>
  /// executes the task
  /// </summary>
  /// <returns></returns>
  public abstract Task Execute();
}
/// <summary>
/// will start empty with NoTask then as a task is updated it will be switched between Scheduled and Done
/// </summary>
public enum HangfireTaskStatus {
  NoTask,
  Scheduled,
  Done,
  /// <summary>
  /// To use only in Cache
  /// </summary>
  Processing}
