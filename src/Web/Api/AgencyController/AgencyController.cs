using Web.ControllerServices;
using MediatR;
using Web.ControllerServices.QuickServices;
using Microsoft.AspNetCore.Authorization;

namespace Web.Api.Agencycontroller;

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
