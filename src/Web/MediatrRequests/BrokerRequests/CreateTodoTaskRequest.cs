using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Web.ApiModels.RequestDTOs;
using MediatR;

namespace Web.MediatrRequests.BrokerRequests;

public class CreateTodoTaskRequest : IRequest<ToDoTaskWithLeadName>
{
  public Guid BrokerID { get; set; }
  public CreateToDoTaskDTO createToDoTaskDTO { get; set; }
}
public class CreateTodoTaskRequestHandler : IRequestHandler<CreateTodoTaskRequest, ToDoTaskWithLeadName>
{
  private readonly AppDbContext _appDbContext;
  public CreateTodoTaskRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<ToDoTaskWithLeadName> Handle(CreateTodoTaskRequest request, CancellationToken cancellationToken)
  {
    var todo = new ToDoTask
    {
      BrokerId = request.BrokerID,
      LeadId = request.createToDoTaskDTO.leadId,
      TaskDueDate = request.createToDoTaskDTO.dueTime,
      Description = request.createToDoTaskDTO.Description,
      TaskName = request.createToDoTaskDTO.TaskName,
    };
    _appDbContext.ToDoTasks.Add(todo);
    await _appDbContext.SaveChangesAsync();

    var reponse = new ToDoTaskWithLeadName
    { Description = todo.Description,
      Id = todo.Id ,
      TaskDueDate = todo.TaskDueDate.UtcDateTime,
      TaskName = todo.TaskName
    };
    if (todo.LeadId != null)
    {
      //TODO select just first and last names
      var leadSelected = _appDbContext.Leads.First(l => l.Id == todo.LeadId);
      reponse.firstName = leadSelected.LeadFirstName;
      reponse.lastName = leadSelected.LeadLastName;
    }
    return reponse;
  }
}

