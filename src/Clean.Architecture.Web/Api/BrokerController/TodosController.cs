using Clean.Architecture.Web.ApiModels.APIResponses.Broker;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.BrokerRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BrokerController;

[Authorize]
public class TodosController : BaseApiController
{
  private readonly ILogger<TodosController> _logger;
  public TodosController(AuthorizationService authorizeService, IMediator mediator, ILogger<TodosController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpGet("Get-ToDos")]
  public async Task<IActionResult> GetBrokerToDos()
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

    var todos = await _mediator.Send(new GetBrokerTodosRequest { BrokerId = brokerId });
    if (todos == null || !todos.Any()) return NotFound();
    var response = new TodoTasksDTO { todos = todos };
    return Ok(response);
  }

  [HttpPost("Create-ToDo")]
  public async Task<IActionResult> CreateBrokerToDo([FromBody] CreateToDoTaskDTO createToDoTaskDTO)
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

    await _mediator.Send(new CreateTodoTaskRequest
    {
      BrokerID = brokerId,
      createToDoTaskDTO = createToDoTaskDTO
    });

    return Ok();
  }
}
