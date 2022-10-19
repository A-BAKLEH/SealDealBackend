using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Constants;
using Clean.Architecture.Web.ApiModels.APIResponses.Templates;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BrokerController;
[Authorize]
public class TemplatesController : BaseApiController
{
  private readonly ILogger<TemplatesController> _logger;
  //private readonly BrokerTagsQService _brokerTagsQService;
  //private readonly AgencyQService _agencyQService;
  private readonly TemplatesQService _templatesQService;
  public TemplatesController(AuthorizationService authorizeService, IMediator mediator,
    ILogger<TemplatesController> logger, TemplatesQService templatesQService) : base(authorizeService, mediator)
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


  [HttpGet("AllTemplates")]
  public async Task<IActionResult> GetAllTemplates()
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }

    var templatesDTO = _templatesQService.GetAllTemplatesAsync(id);
    return Ok(templatesDTO);
  }

  [HttpPost("Template")]
  public async Task<IActionResult> CreateBrokerTemplate([FromBody] CreateTemplateDTO dto)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if ( !brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] inactive mofo User with UserId {UserId} tried to get Listings", TagConstants.Inactive, id);
      return Unauthorized();
    }

    var template = await _templatesQService.CreateTemplateAsync(dto, id);

    return Ok(template);
  }

}
