using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BrokerController;
public class BrokerController : BaseApiController
{
  public BrokerController(AuthorizeService authorizeService) : base(authorizeService)
  {
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
