using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.ApiModels.RequestDTOs;

namespace Web.MediatrRequests.BrokerRequests;

public class CreateTodoTaskRequest : IRequest<ToDoTaskWithLeadName>
{
    public Guid BrokerID { get; set; }
    public CreateToDoTaskDTO createToDoTaskDTO { get; set; }
}
public class CreateTodoTaskRequestHandler : IRequestHandler<CreateTodoTaskRequest, ToDoTaskWithLeadName>
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<CreateTodoTaskRequestHandler> _logger;
    public CreateTodoTaskRequestHandler(AppDbContext appDbContext, ILogger<CreateTodoTaskRequestHandler> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task<ToDoTaskWithLeadName> Handle(CreateTodoTaskRequest request, CancellationToken cancellationToken)
    {

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

        //add calendar if broker wants to
        if(request.createToDoTaskDTO.AddToCalendar)
        {
            var broker = await _appDbContext.Brokers
                .Include(b => b.ConnectedEmails.Where(e => e.isCalendar))
                .FirstAsync(b => b.Id == request.BrokerID);
            if(broker.ConnectedEmails == null || broker.ConnectedEmails.Count == 0)
            {
                _logger.LogError("adding event to calendar but no calendar connected email");
            }
            else
            {
                var cred = GoogleCredential.FromAccessToken(broker.ConnectedEmails.First().AccessToken);
                var _CalendarService = new CalendarService(new BaseClientService.Initializer { HttpClientInitializer = cred });

                var res = _CalendarService.Events.Insert(new Event
                {
                    Summary = todo.TaskName,
                    Start = new EventDateTime
                    {
                        DateTimeDateTimeOffset = dueTime
                    },
                    End = new EventDateTime
                    {
                        DateTimeDateTimeOffset = dueTime + TimeSpan.FromMinutes(1)
                    },
                    Description = request.createToDoTaskDTO.leadfirstName == null ? todo.Description : "lead: " + request.createToDoTaskDTO.leadfirstName + "\n" + todo.Description
                }, "primary").Execute();
                todo.CalendarEvenId = res.Id;
            }
        }

        await _appDbContext.SaveChangesAsync();

        var reponse = new ToDoTaskWithLeadName
        {
            Description = todo.Description,
            Id = todo.Id,
            TaskDueDate = todo.TaskDueDate,
            TaskName = todo.TaskName
        };
        if (todo.LeadId != null)
        {
            //var leadSelected = _appDbContext.Leads.Select(l => new { l.Id, l.LeadFirstName, l.LeadLastName }).First(l => l.Id == todo.LeadId);
            reponse.firstName = request.createToDoTaskDTO.leadfirstName;
            reponse.lastName = request.createToDoTaskDTO.leadlastName;
        }
        return reponse;
    }
}

