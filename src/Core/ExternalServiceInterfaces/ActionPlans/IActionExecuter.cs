using Core.Domain.BrokerAggregate.Templates;

namespace Core.ExternalServiceInterfaces.ActionPlans;
public interface IActionExecuter
{
  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// 
  /// 0) ActionPlanAssociation - ActionPlanAssociation
  /// 1) List<ActionBase> - actions
  /// 2) Guid - brokerId
  /// 3) DateTime - timeNow
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  Task<bool> ExecuteChangeLeadStatus(params Object[] pars);
  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  Task<Tuple<bool,EmailTemplate?>> ExecuteSendEmail(params Object[] pars);
  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  Task<bool> ExecuteSendSms(params Object[] pars);
}
