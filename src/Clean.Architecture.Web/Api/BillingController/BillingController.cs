
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.MediatrRequests.StripeRequests;
using Clean.Architecture.SharedKernel.Repositories;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ApiModels.APIResponses;
using Clean.Architecture.Web.ControllerServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BillingController;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillingController : BaseApiController
{

  private readonly IRepository<Broker> _repository;
  private readonly ILogger<BillingController> _logger;
  public BillingController(IRepository<Broker> repository, AuthorizationService authorizeService, IMediator mediator, ILogger<BillingController> logger) : base(authorizeService, mediator)
  {
    _repository = repository;
    _logger = logger;
  }

  [HttpPost("customer-portal")]
  public async Task<IActionResult> CreateBillingPortal([FromBody] CustomerPortalRequestDTO req)
  {
    
    var brokerTuple = await this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));
    if (!brokerTuple.Item3)
    {
      _logger.LogWarning("[{Tag}] non-admin mofo User with UserId {UserId} tried to create Billing portal session",TagConstants.Unauthorized, brokerTuple.Item1.Id.ToString());
      return Forbid();
    }

    var l = User.Claims.ToList();
    Guid b2cBrokerId = brokerTuple.Item1.Id;

    var portalURL = await _mediator.Send(
      new CreateBillingPortalRequest
      {
        BrokerId = b2cBrokerId,
        returnURL = req.ReturnUrl
      });
    _logger.LogInformation("[{Tag}] created Billing portal for user with UserId {UserId} ", TagConstants.BillingPortal,b2cBrokerId);
    return Ok(new BillingPortalResponse
    {
      portalURL = portalURL
    });
  }
}
