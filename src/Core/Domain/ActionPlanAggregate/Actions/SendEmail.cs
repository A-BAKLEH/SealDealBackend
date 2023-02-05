namespace Core.Domain.ActionPlanAggregate.Actions;
public class SendEmail : ActionBase
{
  
  public const string EmailTemplateId = "EmailTemplateId"; //int value 

  public async override Task<bool> Execute(params object[] pars)
  {
    return await _IActionExecuter.ExecuteSendEmail(pars);
  }
}
