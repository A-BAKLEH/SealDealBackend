
namespace Core.Domain.ActionPlanAggregate.Actions;
public class ChangeLeadStatus : ActionBase
{
  public const string NewLeadStatus = "NewLeadStatus"; //parse value into enum of type LeadStatus

  /// <summary>
  /// 
  /// outside inputs: LeadId
  /// </summary>
  public async override Task<Tuple<int?, string?>> Execute()
  {

    Console.WriteLine("executing from ChangeStatusAction");
    return null;
  }
}
