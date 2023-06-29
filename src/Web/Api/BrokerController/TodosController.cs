using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.ApiModels.APIResponses.Broker;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using Web.MediatrRequests.BrokerRequests;

namespace Web.Api.BrokerController;

[Authorize]
public class TodosController : BaseApiController
{
    private readonly ILogger<TodosController> _logger;
    private readonly ToDoTaskQService _toDoTaskQService;
    public TodosController(AuthorizationService authorizeService,
      IMediator mediator, ILogger<TodosController> logger,
      ToDoTaskQService doTaskQService) : base(authorizeService, mediator)
    {
        _logger = logger;
        _toDoTaskQService = doTaskQService;
    }

    [HttpGet("MyTodos")]
    public async Task<IActionResult> GetBrokerToDos()
    {
        var brokerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(brokerId);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }

        var todos = await _mediator.Send(new GetBrokerTodosRequest { BrokerId = brokerId });
        if (todos == null || !todos.Any()) return NotFound();

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        foreach (var todo in todos)
        {
            todo.TaskDueDate = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, todo.TaskDueDate);
        }

        var response = new TodoTasksDTO
        {
            todos = todos
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrokerToDo([FromBody] CreateToDoTaskDTO createToDoTaskDTO)
    {
        var brokerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(brokerId);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }
        var oldDate = createToDoTaskDTO.dueTime;
        var timeZone = createToDoTaskDTO.TempTimeZone ?? brokerTuple.Item1.TimeZoneId;
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        createToDoTaskDTO.dueTime = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, createToDoTaskDTO.dueTime);

        var todo = await _mediator.Send(new CreateTodoTaskRequest
        {
            BrokerID = brokerId,
            createToDoTaskDTO = createToDoTaskDTO,
        });
        todo.TaskDueDate = oldDate;
        return Ok(todo);
    }


    [HttpDelete("{TaskId}")]
    public async Task<IActionResult> DeleteToDoTask(int TaskId)
    {
        var brokerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(brokerId);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }
        await _toDoTaskQService.DeleteToDoAsync(TaskId, brokerId);
        return Ok();
    }

}
