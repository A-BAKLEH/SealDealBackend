using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;

namespace Web.Api.NotificationsController;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly ILogger<NotificationsController> _logger;
    public NotificationsController(AuthorizationService authorizeService, IMediator mediator, ILogger<NotificationsController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
    }

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("BrokerNotifs")]
    public async Task<IActionResult> GetBrokerNotifs()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to GetNotifs ", TagConstants.Unauthorized, id);
            return Forbid();
        }
        return Ok();
    }


}
