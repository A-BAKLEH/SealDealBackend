using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.LeadRequests;
using Clean.Architecture.Web.MediatrRequests.NotifsRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.LeadController;

[Authorize]
public class LeadController : BaseApiController
{
  private readonly ILogger<LeadController> _logger;
  public LeadController(AuthorizationService authorizeService, IMediator mediator, ILogger<LeadController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  //new
  [HttpPost("Create-Lead")]
  public async Task<IActionResult> CreateLead([FromBody] IEnumerable<CreateLeadDTO> createLeadDTO)
  {
    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    var brokerTuple = await this._authorizeService.AuthorizeUser(id);
    if (!brokerTuple.Item2)
    {
      _logger.LogWarning("[{Tag}] Inactive User with UserId {UserId} tried to create Lead", TagConstants.Unauthorized, id);
      return Unauthorized();
    }
    var broker = brokerTuple.Item1;
    await _mediator.Send(new CreateLeadRequest
    {
      AgencyId = broker.AgencyId,
      BrokerId = broker.Id,
      createLeadDTOs = createLeadDTO
    });

    return Ok();
  }
}
