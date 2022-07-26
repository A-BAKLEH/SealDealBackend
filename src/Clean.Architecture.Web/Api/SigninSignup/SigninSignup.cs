using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.Commands_Handlers.Signup;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.SigninSignup;

public class SigninSignup : BaseApiController
{

  public SigninSignup(AuthorizeService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
  }

  /// <summary>
  /// if an existing user signs in, returns OK(false);
  /// if a new user signs up, insert Agency and Admin Broker into DB and return OK(true)
  /// </summary>
  /// <returns></returns>
  [HttpGet("signin-signup")]
  public async Task<IActionResult> SigninSingup()
  {

    //var broker = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));
    bool signup = false;
    try
    {
      var auth = User.Identity.IsAuthenticated;
      if (!auth)
      {
        throw new Exception("not auth");
      }

      var l = User.Claims.ToList();
      var findClaim = l.Find(x => x.Type == "newUser");
      if (findClaim == null) return Ok(signup);

      signup = true;

      await _mediator.Send(new SignupCommand
      {
        AgencyName = l.Find(x => x.Type == "extension_AgencyName").Value,
        givenName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value,
        surName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value,
        b2cId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value),
        email = l.Find(x => x.Type == "emails").Value
      });
    }
    catch (Exception ex)
    {
      //log error 
      return BadRequest();
    }

    return Ok(signup);
  }
}
