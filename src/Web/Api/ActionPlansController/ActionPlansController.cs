using Core.Config.Constants.LoggingConstants;
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
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }
        var result = await _actionPQService.CreateActionPlanAsync(dto, id);

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        result.TimeCreated = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, result.TimeCreated);

        return Ok(result);
    }


    [HttpDelete("{ActionPlanId}")]
    public async Task<IActionResult> Delete(int ActionPlanId)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }
        await _actionPQService.DeleteActionPlanAsync(id, ActionPlanId);
        return Ok();
    }


    [HttpGet]
    public async Task<IActionResult> GetMyAPs()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }

        var result = await _actionPQService.GetMyActionPlansAsync(id);

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        foreach (var item in result)
        {
            item.TimeCreated = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, item.TimeCreated);
        }
        return Ok(result);
    }

    /// <summary>
    /// manually starts an action plan for a lead
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("ManualStart")]
    public async Task<IActionResult> ManualStart([FromBody] StartActionPlanDTO dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }
        var res = await _actionPQService.StartLeadActionPlanManually(id, dto);

        return Ok(res);
    }

    [HttpPatch("ToggleActive")]
    public async Task<IActionResult> ToggleActive([FromBody] PatchActionPlanTriggerDTO dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }
        await _actionPQService.ToggleActiveTriggerAsync(id, dto.Toggle, dto.ActionPlanId);
        return Ok();
    }

    [HttpPatch("Lead/StopActionPlans")]
    public async Task<IActionResult> StopActionPlansOnLead([FromBody] StopActionPlanLeadDTO dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }
        await _actionPQService.StopActionPlansOnALead(id, dto.LeadId);
        return Ok();
    }


    [HttpPatch("SetTrigger")]
    public async Task<IActionResult> SetTrigger([FromBody] ChangeTriggerDTO dto)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User with userId {userId}", TagConstants.Inactive, id);
            return Forbid();
        }
        await _actionPQService.SetNewTriggerAsync(id, dto.NewTrigger, dto.ActionPlanId);
        return Ok();
    }
}