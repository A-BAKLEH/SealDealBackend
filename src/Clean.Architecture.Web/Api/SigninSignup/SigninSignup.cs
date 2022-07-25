using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.SigninSignup;



public class SigninSignup : BaseApiController
{
  public readonly IRepository<Agency> _repo;
  public SigninSignup(AuthorizeService authorizeService, IRepository<Agency> repository) : base(authorizeService)
  {
    _repo = repository;
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

      string AgencyName = l.Find(x => x.Type == "extension_AgencyName").Value;
      string givenName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value;
      string surName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value;
      string email = l.Find(x => x.Type == "emails").Value;
      Guid b2cId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

      var broker = new Broker()
      {
        Id = b2cId,
        FirstName = givenName,
        LastName = surName,
        Email = email,
        isAdmin = true,
      };
      var agency = new Agency()
      {

        AgencyName = AgencyName,
        IsPaying = false,
        SoloBroker = true,
        AgencyBrokers = new List<Core.BrokerAggregate.Broker> { broker }
      };
      await _repo.AddAsync(agency);

    }
    catch (Exception ex)
    {
      //log error 
      return BadRequest();
    }

    return Ok(signup);
  }
}
