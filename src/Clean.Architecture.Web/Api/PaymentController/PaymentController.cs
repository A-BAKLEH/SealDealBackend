using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ApiModels.APIResponses;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.StripeRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Clean.Architecture.Web.Api.Payment;

[Authorize]
public class PaymentController : BaseApiController
{
  private readonly ILogger<PaymentController> _logger;
  public PaymentController(AuthorizationService authorizeService, IMediator mediator, ILogger<PaymentController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
  }

  [HttpPost("create-checkout-session")]
  public async Task<IActionResult> CreateChekoutSession([FromBody] CheckoutSessionRequestDTO req)
  {
    Guid b2cBrokerId;
    int AgencyID;

    var brokerTuple = await this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value),true);
    if (!brokerTuple.Item3)
    {
      _logger.LogWarning("[{Tag}] non-admin mofo User with UserId {UserId} tried to create Checkout Session", TagConstants.Unauthorized, brokerTuple.Item1.Id.ToString());
      return Forbid();
    }
    b2cBrokerId = brokerTuple.Item1.Id;
    AgencyID = brokerTuple.Item1.AgencyId;
    
    _logger.LogInformation("[{Tag}] Creating a CheckoutSession for User with UserId '{UserId}' in" +
      " Agency with AgencyId {AgencyId} with PriceID {PriceID} and Quantity {Quantity}",TagConstants.CheckoutSession,b2cBrokerId.ToString(),AgencyID,req.PriceId, req.Quantity);

    var checkoutSessionDTO = await _mediator.Send(new CreateCheckoutSessionRequest
    {
      agency = brokerTuple.Item1.Agency,
      priceID = req.PriceId,
      Quantity = req.Quantity >= 1 ? req.Quantity : 1,
    });

    if (string.IsNullOrEmpty(checkoutSessionDTO.sessionId)) throw new InconsistentStateException("CreateCheckoutSession-nullOrEmpty SessionID",$"session ID is {checkoutSessionDTO.sessionId}",b2cBrokerId.ToString());
    _logger.LogInformation("[{Tag}] Created a CheckoutSession with ID {CheckoutSessionId} for User with UserId '{UserId}' in " +
      "Agency with AgencyId {AgencyId}", TagConstants.CheckoutSession, checkoutSessionDTO.sessionId, b2cBrokerId.ToString(), AgencyID);
    return Ok(checkoutSessionDTO);
  }
}
