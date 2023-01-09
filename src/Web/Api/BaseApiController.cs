using Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace Web.Api;

/// <summary>
/// If your API controllers will use a consistent route convention and the [ApiController] attribute (they should)
/// then it's a good idea to define and use a common base controller class like this one.
/// </summary>
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
