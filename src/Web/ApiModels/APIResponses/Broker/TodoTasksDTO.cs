using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;

namespace Web.ApiModels.APIResponses.Broker;

public class TodoTasksDTO
{
  public List<ToDoTaskWithLeadName> todos { get; set; }
}


