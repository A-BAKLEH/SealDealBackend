using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;

namespace Web.Api.NotificationsController;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly NotificationService _notificationService;
    public NotificationsController(AuthorizationService authorizeService, NotificationService notificationService, IMediator mediator, ILogger<NotificationsController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// all notifs for signin
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/All")]
    public async Task<IActionResult> GetALLDashboardNotifs()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to GetNotifs ", TagConstants.Unauthorized, id);
            return Forbid();
        }
        //TODO make times local
        var res = await _notificationService.GetAllDashboardNotifs(brokerTuple.Item1.Id);
        return Ok(res);
    }

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Normal")]
    public async Task<IActionResult> GetDashboardNormalTableEvents()
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

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Priority")]
    public async Task<IActionResult> GetDashboardPriorityTableEvents()
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

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/{LeadId}/{Normal}/{Priority}")]
    public async Task<IActionResult> GetDashboardNotifsPerLead(int LeadId, bool Normal, bool Priority)
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

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/Other")]
    public async Task<IActionResult> GetOtherNotifs()
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
    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    //[HttpGet("BrokerNotifs")]
    //public async Task<IActionResult> GetBrokerNotifs()
    //{
    //    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    //    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    //    if (!brokerTuple.Item2)
    //    {
    //        _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to GetNotifs ", TagConstants.Unauthorized, id);
    //        return Forbid();
    //    }
    //    return Ok();
    //}


}
