using Core.Config.Constants.LoggingConstants;
using Core.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TimeZoneConverter;
using Web.ApiModels.APIResponses.Templates;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;

namespace Web.Api.BrokerController;
[Authorize]
public class TemplateController : BaseApiController
{
    private readonly ILogger<TemplateController> _logger;

    private readonly TemplatesQService _templatesQService;
    public TemplateController(AuthorizationService authorizeService, IMediator mediator,
      ILogger<TemplateController> logger, TemplatesQService templatesQService) : base(authorizeService, mediator)
    {
        _logger = logger;
        _templatesQService = templatesQService;
    }

    [HttpGet("Variables")]
    public async Task<IActionResult> GetTemplateVariables()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }
        var res = new TemplateVariablesDTO { variables = TemplateVariables.templateVariables };
        return Ok(res);
    }


    [HttpGet("MyTemplates")]
    public async Task<IActionResult> GetAllTemplates()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive or non-admin mofo User", TagConstants.Inactive);
            return Forbid();
        }

        var templatesDTO = await _templatesQService.GetAllTemplatesAsync(id);

        var timeZoneInfo = TZConvert.GetTimeZoneInfo(brokerTuple.Item1.TimeZoneId);
        foreach (var dto in templatesDTO.allTemplates)
        {
            dto.Modified = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, dto.Modified);
        }
        var res = templatesDTO.allTemplates;
        return Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrokerTemplate([FromBody] CreateTemplateDTO dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive or non-admin mofo User", TagConstants.Inactive);
            return Forbid();
        }

        var template = await _templatesQService.CreateTemplateAsync(dto, id);

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        template.Modified = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, template.Modified);
        return Ok(template);
    }

    [HttpPatch]
    public async Task<IActionResult> updateBrokerTemplate([FromBody] UpdateTemplateDTO dto)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(id);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive or non-admin mofo User", TagConstants.Inactive);
            return Forbid();
        }

        var template = await _templatesQService.UpdateTemplateAsync(dto, id);

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        template.Modified = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, template.Modified);
        return Ok(template);
    }

    //e for email, s for sms
    [HttpDelete("{TemplateType}/{Id}")]
    public async Task<IActionResult> DeleteBrokerTemplate(string TemplateType, int Id)
    {
        var brokerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var brokerTuple = await this._authorizeService.AuthorizeUser(brokerId);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} inactive mofo User", TagConstants.Inactive);
            return Forbid();
        }

        var names = await _templatesQService.DeleteTemplateAsync(Id, TemplateType, brokerId);
        if (names.Any()) return BadRequest(names);
        return Ok();
    }
}
