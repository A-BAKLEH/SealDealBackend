using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.NotificationsController;

[Authorize]
public class NotificationsController : BaseApiController
{
  private readonly ILogger<NotificationsController> _logger;
  public NotificationsController(AuthorizationService authorizeService, IMediator mediator, ILogger<NotificationsController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpGet("Get-Notifs")]
  public async Task<IActionResult> GetBrokerNotifs()
  {
    return Ok();
  }


}
