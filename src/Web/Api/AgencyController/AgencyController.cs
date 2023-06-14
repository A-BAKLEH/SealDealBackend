using MediatR;
using Microsoft.AspNetCore.Authorization;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;

namespace Web.Api.Agencycontroller;

[Authorize]
public class AgencyController : BaseApiController
{
    private readonly ILogger<AgencyController> _logger;
    public AgencyController(AuthorizationService authorizeService, IMediator mediator, ILogger<AgencyController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
    }
}
