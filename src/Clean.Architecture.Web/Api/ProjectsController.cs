
using Clean.Architecture.Core.Domain.ProjectAggregate;
using Clean.Architecture.SharedKernel.Repositories;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api;

/// <summary>
/// A sample API Controller. Consider using API Endpoints (see Endpoints folder) for a more SOLID approach to building APIs
/// https://github.com/ardalis/ApiEndpoints
/// </summary>
public class ProjectsController : BaseApiController
{
  private readonly IRepository<Project> _repository;

  public ProjectsController(IRepository<Project> repository, AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
    _repository = repository;
  }

  // GET: api/Projects
  [HttpGet]
  public async Task<IActionResult> List()
  {
    /*var projectDTOs = (await _repository.ListAsync())
        .Select(project => new ProjectDTO
        (
            id: project.Id,
            name: project.Name
        ))
        .ToList();

    return Ok(projectDTOs);*/
    return Ok();
  }

  // GET: api/Projects
  [HttpGet("{id:int}")]
  public async Task<IActionResult> GetById(int id)
  {
    /*var projectSpec = new ProjectByIdWithItemsSpec(id);
    var project = await _repository.GetBySpecAsync(projectSpec);
    if (project == null)
    {
      return NotFound();
    }

    var result = new ProjectDTO
    (
        id: project.Id,
        name: project.Name,
        items: new List<ToDoItemDTO>
        (
            project.Items.Select(i => ToDoItemDTO.FromToDoItem(i)).ToList()
        )
    );

    return Ok(result);*/
    return Ok();
  }

  // POST: api/Projects
  [HttpPost]
  public async Task<IActionResult> Post([FromBody] CreateProjectDTO request)
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

  // PATCH: api/Projects/{projectId}/complete/{itemId}
  [HttpPatch("{projectId:int}/complete/{itemId}")]
  public async Task<IActionResult> Complete(int projectId, int itemId)
  {
    /*var projectSpec = new ProjectByIdWithItemsSpec(projectId);
    var project = await _repository.GetBySpecAsync(projectSpec);
    if (project == null) return NotFound("No such project");

    var toDoItem = project.Items.FirstOrDefault(item => item.Id == itemId);
    if (toDoItem == null) return NotFound("No such item.");

    toDoItem.MarkComplete();
    await _repository.UpdateAsync(project);

    return Ok();*/
    return Ok();
  }
}
