namespace Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
public class SendEmailAction : ActionBase
{
  
  public const string EmailTemplateIdKey = "EmailTemplateId"; //int value
  public const string EmailTextKey = "EmailText"; // string value

  public async override Task<Tuple<int?, string?>> Execute()
  {
    Console.WriteLine("executing from SendEmailAction");
    return null;
  }
}
