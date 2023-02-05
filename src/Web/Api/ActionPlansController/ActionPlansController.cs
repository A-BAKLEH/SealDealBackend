using Core.Config.Constants.LoggingConstants;
using Core.Domain.LeadAggregate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ApiModels.RequestDTOs.ActionPlans;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;

namespace Web.Api.ActionPlansController;

[Authorize]
public class ActionPlansController : BaseApiController
{
  private readonly ILogger<ActionPlansController> _logger;
  private readonly ActionPQService _actionPQService;
  public ActionPlansController(AuthorizationService authorizeService, ActionPQService actionPQService, IMediator mediator, ILogger<ActionPlansController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
    _actionPQService = actionPQService;
  }


  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateActionPlanDTO dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to create ActionPlan", TagConstants.Inactive, id);
      return Forbid();
    }

    var result = await _actionPQService.CreateActionPlanAsync(dto,id);

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    result.TimeCreated = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, result.TimeCreated);

    return Ok(result);
  }

  /// <summary>
  /// manually starts an action plan for a lead
  /// </summary>
  /// <param name="dto"></param>
  /// <returns></returns>
  [HttpPost("/ManualStart")]
  public async Task<IActionResult> ManualStart([FromBody] StartActionPlanDTO dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to start actionPlan manually", TagConstants.Inactive, id);
      return Forbid();
    }

     await _actionPQService.StartLeadActionPlanManually(id,dto.LeadId,dto.ActionPlanID, dto.customDelay);

    return Ok();
  }
}
