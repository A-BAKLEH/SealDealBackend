using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.SigninSignup;

[Authorize]
public class AccountVerificationController : BaseApiController
{
  private readonly ILogger<AccountVerificationController> _logger;
  public AccountVerificationController(AuthorizationService authorizeService, IMediator mediator, ILogger<AccountVerificationController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpGet("account-verif")]
  public async Task<IActionResult> VerifyAccount()
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var accountStatus = await this._authorizeService.VerifyAccountAsync(id);
    
    return Ok(accountStatus);
  }
}
