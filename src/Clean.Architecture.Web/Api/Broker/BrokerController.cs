using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.Broker;
public class BrokerController : Controller
{

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
    return Ok();
  }
}
