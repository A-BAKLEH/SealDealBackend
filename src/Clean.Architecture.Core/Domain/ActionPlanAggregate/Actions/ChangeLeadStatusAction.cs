
namespace Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
public class ChangeLeadStatusAction : ActionBase
{
  private const string NewLeadStatusKey = "NewLeadStatus"; //parse value into enum of type LeadStatus
  private const string PreviousLeadStatusKey = "PreviousLeadStatus"; //parse value into enum of type LeadStatus

  /// <summary>
  /// 
  /// outside inputs: LeadId
  /// </summary>
  public async override Task<Tuple<int?, string?>> Execute()
  {

    Console.WriteLine("executing from SendEmailAction");
    return null;
  }
}
