using Core.Config.Constants.LoggingConstants;
using Core.Constants;
using Core.Domain.BrokerAggregate;
using Web.ApiModels.APIResponses.Templates;
using Web.ApiModels.RequestDTOs;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeZoneConverter;

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
    var res = new TemplateVariablesDTO { variables = TemplateVariables.templateVariables };
    return Ok(res);
  }


  [HttpGet("MyTemplates")]
  public async Task<IActionResult> GetAllTemplates()
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
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
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if ( !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
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
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to update template", TagConstants.Inactive, id);
      return Forbid();
    }

    var template = await _templatesQService.UpdateTemplateAsync(dto, id);

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
    template.Modified = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, template.Modified);
    return Ok(template);
  }

}
