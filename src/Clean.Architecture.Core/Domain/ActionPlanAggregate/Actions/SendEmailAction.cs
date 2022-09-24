namespace Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions;
public class SendEmailAction : ActionBase
{
  
  private const string EmailTemplateIdKey = "EmailTemplateId"; //int value
  private const string EmailTextKey = "EmailText"; // string value

  public async override Task<Tuple<int?, string?>> Execute()
  {
    Console.WriteLine("executing from SendEmailAction");
    return null;
  }
}
