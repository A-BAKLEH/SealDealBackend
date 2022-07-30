using Clean.Architecture.Core.Commands_Handlers.Stripe;
using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Clean.Architecture.Web.Api.Payment;

public class PaymentController : BaseApiController
{
  private readonly IRepository<CheckoutSession> _repository;

  public PaymentController(IRepository<CheckoutSession> repository, AuthorizeService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
    _repository = repository;
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
      throw new Exception($"authentication and/or b2c admin Guid Id retrieval failed, error m :\n {ex}");
    }

    var checkoutSessionID = await _mediator.Send(new CheckoutSessionCommand
    {
      adminId = b2cBrokerId,
      AgencyID = AgencyID,
      priceID = req.PriceId,
      Quantity = req.Quantity
    });

    return Ok(new CheckoutSessionResponseDTO { SessionId = checkoutSessionID});
  }
}
