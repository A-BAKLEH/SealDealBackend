namespace Core.Domain.ActionPlanAggregate.Actions;
public class SendEmail : ActionBase
{
  
  public const string EmailTemplateId = "EmailTemplateId"; //int value 

  public async override Task<Tuple<bool, object?>> Execute(params object[] pars)
  {
    var b = await _IActionExecuter.ExecuteSendEmail(pars);
    return Tuple.Create<bool, object?>(b, null);
  }
}
