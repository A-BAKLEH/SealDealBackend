using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BrokerController;
public class BrokerController : BaseApiController
{
  public BrokerController(AuthorizeService authorizeService) : base(authorizeService)
  {
  }

  [HttpGet("signin-signup")]
  public async Task<IActionResult> SigninSingup()
  {
    var auth = User.Identity.IsAuthenticated;
    if (!auth) throw new Exception("not auth");

    //var broker = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));


    bool signup = false;
    try
    {
      var l = User.Claims.ToList();
      var findClaim = l.Find(x => x.Type == "newUser");
      if (findClaim == null) return Ok(signup);

      signup = true;

      string AgencyName = l.Find(x => x.Type == "extension_AgencyName").Value;
      string givenName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value;
      string surName = l.Find(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value;
      string email = l.Find(x => x.Type == "emails").Value;
      Guid b2cadminId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

      var agency = _agencyRepository.Add(new Agency() { AgencyName = AgencyName, IsPaying = false, SoloBroker = true });
      _adminRepository.Add(new Admin()
      {
        AgencyId = agency.AgencyId,
        BrokerId = b2cadminId,
        FirstName = givenName,
        LastName = surName,
        Email = email,
      });
    }
    catch (Exception ex)
    {
      return BadRequest(ex.Message);
    }

    return Ok(signup);
  }


  [HttpPost]
  public async Task<IActionResult> Post()
  {
    var auth = User.Identity.IsAuthenticated;
    if (!auth) throw new Exception("not auth");

    var broker =  this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));

    /*var newProject = new Project(request.Name, PriorityStatus.Backlog);

    var createdProject = await _repository.AddAsync(newProject);

    var result = new ProjectDTO
    (
        id: createdProject.Id,
        name: createdProject.Name
    );
    return Ok(result);*/
    return Ok();
  }
}
