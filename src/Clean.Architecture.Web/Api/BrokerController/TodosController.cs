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
    //Not checking
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);


    var todos = await _mediator.Send(new GetBrokerTodosRequest { BrokerId = brokerId });
    return Ok(todos);
  }
}
