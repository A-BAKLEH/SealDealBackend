using Clean.Architecture.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Clean.Architecture.Core.AgencyAggregate;
using Microsoft.Identity.Web.Resource;
using Clean.Architecture.Web.AuthenticationAuthorization;
using Clean.Architecture.Core.BrokerAggregate;

namespace Clean.Architecture.Web.Api.Agencycontroller;

public class AgencyController : BaseApiController
{
  private readonly IRepository<Agency> _repository;

  public AgencyController(IRepository<Agency> repository, AuthorizeService authorizeService) : base(authorizeService)
  {
    _repository = repository;
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

      var broker = new Broker()
      {
        Id = b2cadminId,
        FirstName = givenName,
        LastName = surName,
        Email = email,
        isAdmin = true,
      };
      var agency = new Agency() {
        
        AgencyName = AgencyName,
        IsPaying = false,
        SoloBroker = true,
        AgencyBrokers = new List<Core.BrokerAggregate.Broker> { broker}
      };
      await _repository.AddAsync(agency);
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
    /*var newProject = new Project(request.Name, PriorityStatus.Backlog);

    var createdProject = await _repository.AddAsync(newProject);

    var result = new ProjectDTO
    (
        id: createdProject.Id,
        name: createdProject.Name
    );
    return Ok(result);*/
    var newAgnency = new Agency()
    {
      AgencyName = "lolagency",
      IsPaying = false,
      SoloBroker = false
    };
    await _repository.AddAsync(newAgnency);
    return Ok();
  }
}
