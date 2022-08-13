using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.TestingAPI;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{


  public TestController()
  {

  }

  [HttpGet("test-signup")]
  public async Task<IActionResult> SigninSingupTest()
  {

    //var broker = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));
    //bool signup = false;
    try
    {
      var auth = User.Identity.IsAuthenticated;
      if (!auth)
      {
        throw new Exception("not auth");
      }

      var l = User.Claims.ToList();
      var findClaim = l.Find(x => x.Type == "newUser");
      //signin only
      if (findClaim == null)
      {
        Guid b2cID = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        return Ok(_authorizeService.signinSignupUser(b2cID));
      }

      //signup
      var signinResponseDTO = _mediator.Send(new SignupRequest
      {
        AgencyName = l.Find(x => x.Type == "extension_AgencyName").Value,
        givenName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value,
        surName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value,
        b2cId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value),
        email = l.Find(x => x.Type == "emails").Value
      }).Result;
      return Ok(new SigninResponse
      {
        SubscriptionStatus = signinResponseDTO.SubscriptionStatus,
        UserAccountStatus = signinResponseDTO.UserAccountStatus
      });
    }
    catch (Exception ex)
    {
      //log error
      return BadRequest();
    }
  }
}
