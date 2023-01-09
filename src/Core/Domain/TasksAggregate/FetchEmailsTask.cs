namespace Core.Domain.TasksAggregate;
public class FetchEmailsTask : RecurrentTaskBase
{
  public string? LastEmailToken{get; set;}

  public override Task Execute()
  {
    throw new NotImplementedException();
  }
}
