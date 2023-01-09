using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Web.ApiModels.APIResponses.Broker;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.ControllerServices.StaticMethods;
using Clean.Architecture.Web.MediatrRequests.BrokerRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeZoneConverter;

namespace Clean.Architecture.Web.Api.BrokerController;

[Authorize]
public class TodosController : BaseApiController
{
  private readonly ILogger<TodosController> _logger;
  public TodosController(AuthorizationService authorizeService, IMediator mediator, ILogger<TodosController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpGet("MyTodos")]
  public async Task<IActionResult> GetBrokerToDos()
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(brokerId);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to create todo", TagConstants.Inactive, brokerId);
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
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(brokerId);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to create todo", TagConstants.Inactive, brokerId);
      return Forbid();
    }
    var oldDate = createToDoTaskDTO.dueTime;
    var timeZone = createToDoTaskDTO.TempTimeZone ?? brokerTuple.Item1.TimeZoneId;
    var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
    createToDoTaskDTO.dueTime = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, createToDoTaskDTO.dueTime);

    var todo = await _mediator.Send(new CreateTodoTaskRequest
    {
      BrokerID = brokerId,
      createToDoTaskDTO = createToDoTaskDTO,
    });
    todo.TaskDueDate = oldDate;
    return Ok(todo);
  }
}
