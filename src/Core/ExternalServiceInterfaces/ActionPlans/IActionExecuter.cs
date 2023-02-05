namespace Core.ExternalServiceInterfaces.ActionPlans;
public interface IActionExecuter
{
  Task ExecuteChangeLeadStatus();

  Task ExecuteSendEmail();
  Task ExecuteSendSms();
}
