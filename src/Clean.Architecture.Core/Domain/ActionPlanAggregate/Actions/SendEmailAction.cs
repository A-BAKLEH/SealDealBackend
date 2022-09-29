namespace Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
public class SendEmailAction : ActionBase
{
  
  public const string EmailTemplateIdKey = "EmailTemplateId"; //int value
  public const string EmailTextKey = "EmailText"; // string value

  public async override Task<Tuple<int?, string?>> Execute()
  {
    Console.WriteLine("executing from SendEmailAction");
    //call IEmailSender or some other interface to a method that is overloaded/specific for sending
    //emails with required inputs that you send from here. Implement the interface in 

    //the email sent operation will success with returned Id of email sent -> Update ActionTracker and
    // schedule next action/ stop action plan.
    return null;
  }
}
