using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;

namespace Clean.Architecture.Web.ApiModels.APIResponses.Broker;

public class TodoTasksDTO
{
  public List<ToDoTaskWithLeadName> todos { get; set; }
}


