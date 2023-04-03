using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using MediatR;
using Web.ApiModels.RequestDTOs;
using Web.Processing.Various;

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
        //TODO make sure reminders always happen
        using var transaction = _appDbContext.Database.BeginTransaction();

        var dueTime = request.createToDoTaskDTO.dueTime;
        var todo = new ToDoTask
        {
            BrokerId = request.BrokerID,
            LeadId = request.createToDoTaskDTO.leadId,
            TaskDueDate = dueTime,
            Description = request.createToDoTaskDTO.Description,
            TaskName = request.createToDoTaskDTO.TaskName,
        };
        _appDbContext.ToDoTasks.Add(todo);
        await _appDbContext.SaveChangesAsync();

        var firstReminder = dueTime - TimeSpan.FromMinutes(16);
        var HangfireJobId1 = Hangfire.BackgroundJob.Schedule<HandleTodo>(h => h.Handle(todo.Id, 1), firstReminder);
        todo.HangfireReminderId = HangfireJobId1;
        await _appDbContext.SaveChangesAsync();
        transaction.Commit();

        var reponse = new ToDoTaskWithLeadName
        {
            Description = todo.Description,
            Id = todo.Id,
            TaskDueDate = todo.TaskDueDate.UtcDateTime,
            TaskName = todo.TaskName
        };
        if (todo.LeadId != null)
        {
            //TODO maybe add Lead name to Cache
            var leadSelected = _appDbContext.Leads.Select(l => new { l.Id, l.LeadFirstName, l.LeadLastName }).First(l => l.Id == todo.LeadId);
            reponse.firstName = leadSelected.LeadFirstName;
            reponse.lastName = leadSelected.LeadLastName;
        }
        return reponse;
    }
}

