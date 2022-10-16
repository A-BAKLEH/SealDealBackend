using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Clean.Architecture.Web.Api.BrokerController;
[Authorize]
public class TemplatesController : BaseApiController
{
  private readonly ILogger<TemplatesController> _logger;
  //private readonly BrokerTagsQService _brokerTagsQService;
  //private readonly AgencyQService _agencyQService;
  public TemplatesController(AuthorizationService authorizeService, IMediator mediator,
    ILogger<TemplatesController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }


}
