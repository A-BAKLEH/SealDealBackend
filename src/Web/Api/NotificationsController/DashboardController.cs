using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;

namespace Web.Api.NotificationsController;

[Authorize]
public class DashboardController : BaseApiController
{
    private readonly ILogger<DashboardController> _logger;
    private readonly LeadQService _leadQService;
    public DashboardController(AuthorizationService authorizeService,LeadQService leadQService, ILogger<DashboardController> logger, IMediator mediator) : base(authorizeService, mediator)
    {
        _logger = logger;
        _leadQService = leadQService;
    }

    [HttpGet("LeadStats")]
    public async Task<IActionResult> GetLeadStats()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} Inactive User with UserId {userId} tried to GetNotifs ", TagConstants.Unauthorized, id);
            return Forbid();
        }
        
        var res = await _leadQService.GetDsahboardLeadStatsAsync(id);

        return Ok(res);
    }
}
