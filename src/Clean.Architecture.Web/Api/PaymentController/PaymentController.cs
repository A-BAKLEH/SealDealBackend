using Clean.Architecture.Core.Commands_Handlers.StripeCommands;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.ApiModels.Responses;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Clean.Architecture.Web.Api.Payment;

public class PaymentController : BaseApiController
{

  public PaymentController( AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
  }

  [Authorize]
  [HttpPost("create-checkout-session")]
  public async Task<IActionResult> CreateChekoutSession([FromBody] CheckoutSessionRequestDTO req)
  {
    Guid b2cBrokerId;
    int AgencyID;
    try
    {
      var auth = User.Identity.IsAuthenticated;
      if (!auth) throw new Exception("not auth");

      var brokerTuple = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));
      if (!brokerTuple.Item3) return BadRequest("not admin");

      b2cBrokerId = brokerTuple.Item1.Id;
      AgencyID = brokerTuple.Item1.AgencyId;
    }
    catch (Exception ex)
    {
      //log
      throw new Exception($"authentication and/or b2c admin Guid Id retrieval failed, error m :\n {ex}");
    }

    //return sessionID
    var sessionID = await _mediator.Send(new CheckoutSessionCommand
    {
      adminId = b2cBrokerId,
      AgencyID = AgencyID,
      priceID = req.PriceId,
      Quantity = req.Quantity >= 1 ? req.Quantity : 1,
    });

    return Ok(new CheckoutSessionResponse { SessionId = sessionID});
  }
}
