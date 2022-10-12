using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using MediatR;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;

public class CreateTodoTaskRequest : IRequest
{
  public Guid BrokerID { get; set; }
  public CreateToDoTaskDTO createToDoTaskDTO { get; set; }
}
public class CreateTodoTaskRequestHandler : IRequestHandler<CreateTodoTaskRequest>
{
  private readonly AppDbContext _appDbContext;
  public CreateTodoTaskRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<Unit> Handle(CreateTodoTaskRequest request, CancellationToken cancellationToken)
  {
    _appDbContext.ToDoTasks.Add(new ToDoTask
    {
      BrokerId = request.BrokerID,
      LeadId = request.createToDoTaskDTO.leadId,
      TaskDueDate = request.createToDoTaskDTO.dueTime,
      Description = request.createToDoTaskDTO.Description,
      TaskName = request.createToDoTaskDTO.TaskName
    });
    await _appDbContext.SaveChangesAsync();
    return Unit.Value;
  }
}

