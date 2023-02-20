namespace Core.Domain.ActionPlanAggregate.Actions;
public class SendSms : ActionBase
{
  public const string SmsTemplateId = "SmsTemplateId"; //int value 

  public async override Task<Tuple<bool, object?>> Execute(params object[] pars)
  {
    var b = await _IActionExecuter.ExecuteSendSms(pars);
    return Tuple.Create<bool,object?>(b, null);
  }
}
