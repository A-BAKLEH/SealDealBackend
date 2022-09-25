using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.SignupRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.SigninSignup;

[Authorize]
public class SigninSignupController : BaseApiController
{
  private readonly ILogger<SigninSignupController> _logger;
  public SigninSignupController(AuthorizationService authorizeService, IMediator mediator, ILogger<SigninSignupController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpGet("signin-signup")]
  public async Task<IActionResult> SigninSingup()
  {
    var l = User.Claims.ToList();
    var newUserClaim = l.Find(x => x.Type == "newUser");
    //signin only
    if (newUserClaim == null)
    {
      Guid b2cID = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
      var res = await _authorizeService.signinSignupUserAsync(b2cID);
      return Ok(res);
    }
    //signup
    var agencyName = l.Find(x => x.Type == "extension_AgencyName").Value;
    var id = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var signinResponseDTO = await _mediator.Send(new SignupRequest
    {
      AgencyName = agencyName,
      givenName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value,
      surName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value,
      b2cId = id,
      email = l.Find(x => x.Type == "emails").Value
    });
    _logger.LogInformation("[{Tag}] New Agency Signed up with name {AgencyName} and admin B2cId {UserId}", TagConstants.AgencySignup,agencyName,id);
    return Ok(signinResponseDTO);
  }
}
