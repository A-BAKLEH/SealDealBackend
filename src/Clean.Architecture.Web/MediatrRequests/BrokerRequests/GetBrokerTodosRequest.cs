using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;
public class GetBrokerTodosRequest : IRequest<List<ToDoTaskWithLeadName>>
{
  public Guid BrokerId { get; set; }
}
public class GetBrokerTodosRequestHandler : IRequestHandler<GetBrokerTodosRequest, List<ToDoTaskWithLeadName>>
{
  private readonly AppDbContext _appDbContext;
  public GetBrokerTodosRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  //TODO optimize
  public async Task<List<ToDoTaskWithLeadName>> Handle(GetBrokerTodosRequest request, CancellationToken cancellationToken)
  {
    var todos = await _appDbContext.ToDoTasks.Where(todo => todo.BrokerId == request.BrokerId)
      .Select(todoTask => new ToDoTaskWithLeadName
      { 
        Description = todoTask.Description,
        firstName = todoTask.Lead.LeadFirstName,
        lastName = todoTask.Lead.LeadLastName,
        Id = todoTask.Id,
        LeadId = todoTask.LeadId,
        TaskDueDate = todoTask.TaskDueDate,
        TaskName= todoTask.TaskName,
      })
      .ToListAsync();
    return todos;
  }
}

