
namespace Core.Domain.TasksAggregate;
public class OutboxDictsTask : RecurrentTaskBase
{
  /// <summary>
  /// Schedules events that are in Outbox Mem ErrorDictionary 
  /// and handles? jobs in the ScheduledDictionary
  /// </summary>
  /// <returns></returns>
  /// <exception cref="NotImplementedException"></exception>
  public override Task Execute()
  {
    throw new NotImplementedException();
  }
}
