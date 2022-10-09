using Clean.Architecture.Core.Domain.BrokerAggregate;

namespace Clean.Architecture.Web.ApiModels.APIResponses.Broker;

public class TodoTasksDTO
{
  public List<ToDoTask> todos { get; set; }
}
