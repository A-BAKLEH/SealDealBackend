

namespace Core.Domain.ActionPlanAggregate.Actions;
public class SendSms : ActionBase
{
  public const string SmsTemplateId = "SmsTemplateId"; //int value 

  public async override Task<bool> Execute(params object[] pars)
  {
    return await _IActionExecuter.ExecuteSendSms(pars);
  }
}
