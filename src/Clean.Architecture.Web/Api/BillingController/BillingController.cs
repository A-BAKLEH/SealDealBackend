
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ApiModels.APIResponses;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.StripeRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BillingController;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillingController : BaseApiController
{
  private readonly ILogger<BillingController> _logger;
  public BillingController( AuthorizationService authorizeService, IMediator mediator, ILogger<BillingController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpPost("customer-portal")]
  public async Task<IActionResult> CreateBillingPortal([FromBody] CustomerPortalRequestDTO req)
  {
    var brokerTuple = await this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value), true);
    if (!brokerTuple.Item3)
    {
      _logger.LogWarning("[{Tag}] non-admin mofo User with UserId {UserId} tried to create Billing portal session",TagConstants.Unauthorized, brokerTuple.Item1.Id.ToString());
      return Forbid();
    }
    Guid b2cBrokerId = brokerTuple.Item1.Id;

    var portalURL = await _mediator.Send(
      new CreateBillingPortalRequest
      {
        AgencyStripeId = brokerTuple.Item1.Agency.StripeSubscriptionId,
        returnURL = req.ReturnUrl
      });
    _logger.LogInformation("[{Tag}] created Billing portal for user with UserId {UserId} ", TagConstants.BillingPortal,b2cBrokerId);
    return Ok(new BillingPortalResponse
    {
      portalURL = portalURL
    });
  }
}
