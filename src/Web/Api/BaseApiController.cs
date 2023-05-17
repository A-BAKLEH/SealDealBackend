using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;

namespace Web.Api;

[Route("api/[controller]")]
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    public readonly AuthorizationService _authorizeService;
    public readonly IMediator _mediator;
    public BaseApiController(AuthorizationService authorizeService, IMediator mediator)
    {
        _authorizeService = authorizeService;
        _mediator = mediator;
    }
}
