using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Microsoft.AspNetCore.Authorization;

namespace Clean.Architecture.Web.Api.Agencycontroller;

[Authorize]
public class AgencyController : BaseApiController
{
  private readonly ILogger<AgencyController> _logger;
  private readonly AgencyQService _agencyQService;
  public AgencyController( AuthorizationService authorizeService,AgencyQService agencyQService, IMediator mediator, ILogger<AgencyController> logger ) : base(authorizeService, mediator)
  {
    _logger = logger;
    _agencyQService = agencyQService;
  }

}
