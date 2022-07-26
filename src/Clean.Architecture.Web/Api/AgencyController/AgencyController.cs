using Clean.Architecture.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Clean.Architecture.Core.AgencyAggregate;
using Microsoft.Identity.Web.Resource;
using Clean.Architecture.Web.AuthenticationAuthorization;
using Clean.Architecture.Core.BrokerAggregate;
using MediatR;

namespace Clean.Architecture.Web.Api.Agencycontroller;

public class AgencyController : BaseApiController
{


  public AgencyController( AuthorizeService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
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
   /* var newAgnency = new Agency()
    {
      AgencyName = "lolagency",
      IsPaying = false,
      SoloBroker = false
    };
    await _repository.AddAsync(newAgnency);*/
    return Ok();
  }
}
