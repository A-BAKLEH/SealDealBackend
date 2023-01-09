using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.MediatrRequests.BrokerRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.BrokerController;

[Authorize]
public class TagController: BaseApiController
{
  private readonly ILogger<TagController> _logger;
  private readonly TagQService _TagQService;
  public TagController(AuthorizationService authorizeService, IMediator mediator,
    TagQService tagQService,
    ILogger<TagController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
    _TagQService= tagQService;
  }


  [HttpPost("{tagname}")]
  public async Task<IActionResult> CreateTag(string tagname)
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var tagDTO = await _mediator.Send(new CreateBrokerTagRequest { BrokerId = brokerId, TagName = tagname });
    return Ok(tagDTO);
  }

  [HttpGet]
  public async Task<IActionResult> GetTags()
  {
    //Not checking active, permissions
    var brokerId = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

    var tags = await _TagQService.GetBrokerTagsAsync(brokerId);
    if (tags == null) return NotFound();
    return Ok(tags);
  }
}
