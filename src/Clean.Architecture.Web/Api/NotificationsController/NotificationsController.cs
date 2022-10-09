using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Web.ApiModels.APIResponses;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.NotifsRequests;
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

  /// <summary>
  /// To be called by dashboard
  /// </summary>
  /// <returns></returns>
  [HttpGet("Get-Notifs")]
  public async Task<IActionResult> GetBrokerNotifs()
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    if ( !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to GetNotifs ", TagConstants.Unauthorized, id);
      return Unauthorized();
    }
    NotifsResponseDTO response;
    //TODO separate admin request from broker request
    //if not admin
    if( !brokerTuple.Item3 )
    {
      response = await _mediator.Send(new GetNotifsDashboardRequest { BrokerId = id });
    }
    else
    {
      response = await _mediator.Send(new GetNotifsDashboardRequest { BrokerId = id });
    }

    return Ok(response);
  }


}
