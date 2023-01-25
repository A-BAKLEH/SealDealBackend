

namespace Core.Domain.ActionPlanAggregate.Actions;
public class SendSms : ActionBase
{
  public const string SmsTemplateId = "SmsTemplateId"; //int value 

  public async override Task<Tuple<int?, string?>> Execute()
  {
    Console.WriteLine("executing from SendSmsAction");
    //call IEmailSender or some other interface to a method that is overloaded/specific for sending
    //emails with required inputs that you send from here. Implement the interface in 

    //the email sent operation will success with returned Id of email sent -> Update ActionTracker and
    // schedule next action/ stop action plan.
    return null;
  }
}
