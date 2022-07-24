using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace Clean.Architecture.Web.Api;

/// <summary>
/// If your API controllers will use a consistent route convention and the [ApiController] attribute (they should)
/// then it's a good idea to define and use a common base controller class like this one.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[RequiredScope(scopeRequiredByAPI)]
public abstract class BaseApiController : Controller
{
  const string scopeRequiredByAPI = "tasks.read";
  public readonly AuthorizeService _authorizeService;
  public BaseApiController(AuthorizeService authorizeService)
  {
    _authorizeService = authorizeService;
  }
}
