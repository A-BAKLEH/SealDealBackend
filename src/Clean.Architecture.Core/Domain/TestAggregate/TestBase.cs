using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.TestAggregate;
public abstract class TestBase : Entity<int>
{

  //public Dictionary<string, string> ActionProperties { get; set; } = new();
  //public List<ActionTracker> ActionTrackers { get; set; }

  public TestJSON testJSON { get; set; }

  /// <summary>
  /// delay before executing next action
  /// format: Days:hours:minutes 
  /// integer values only
  /// </summary>
  public string? NextActionDelay { get; set; }
  /// <summary>
  /// returns Tuple
  /// T1:ActionResultId for sentEmail for example and T2: string for info
  /// </summary>
  /// <returns></returns>
  public abstract Task<Tuple<int?, string?>> Execute();

}
