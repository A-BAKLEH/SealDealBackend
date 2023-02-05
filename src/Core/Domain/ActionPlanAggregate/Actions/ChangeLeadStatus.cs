
using Core.ExternalServiceInterfaces.ActionPlans;

namespace Core.Domain.ActionPlanAggregate.Actions;
public class ChangeLeadStatus : ActionBase
{
  public const string NewLeadStatus = "NewLeadStatus"; //parse value into enum of type LeadStatus

  /// <summary>
  /// 
  /// outside inputs: LeadId
  /// </summary>
  public async override Task<bool> Execute(params Object[] pars)
  {
    return await _IActionExecuter.ExecuteChangeLeadStatus(pars);
  }
}
