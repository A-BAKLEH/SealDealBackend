
using Core.ExternalServiceInterfaces.ActionPlans;

namespace Core.Domain.ActionPlanAggregate.Actions;
public class ChangeLeadStatus : ActionBase
{
  public const string NewLeadStatus = "NewLeadStatus"; //parse value into enum of type LeadStatus

  /// <summary>
  /// 
  /// outside inputs: LeadId
  /// </summary>
  public async override Task<Tuple<bool, object?>> Execute(params Object[] pars)
  {
    var b = await _IActionExecuter.ExecuteChangeLeadStatus(pars);
    return Tuple.Create<bool,object?>(b,null);
  }
}
