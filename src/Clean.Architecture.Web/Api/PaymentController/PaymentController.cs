using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.Payment;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.ApiModels;
using Clean.Architecture.Web.AuthenticationAuthorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace Clean.Architecture.Web.Api.Payment;
[Route("api/[controller]")]
[ApiController]
public class PaymentController : BaseApiController
{
  private readonly IRepository<CheckoutSession> _repository;

  public PaymentController(IRepository<CheckoutSession> repository, AuthorizeService authorizeService) : base(authorizeService)
  {
    _repository = repository;
  }

  [Authorize]
  [HttpPost("create-checkout-session")]
  public async Task<IActionResult> CreateChekoutSession([FromBody] CheckoutSessionRequestDTO req)
  {
    StripeConfiguration.ApiKey = "sk_test_51LHCXSIAg7HKu3" +
               "TPU6Ess0RMvvdMbFiZw0GwWfgDqZkFUFXtYwTY5XRbjqyJrAnJ8arSQ12k3heATZSbsK6GJyEI00txFG34FH";
    Guid b2cBrokerId;
    try
    {
      var auth = User.Identity.IsAuthenticated;
      if (!auth)
      {
        throw new Exception("not auth");
      }
      var l = User.Claims.ToList();
       b2cBrokerId = Guid.Parse(l.Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    }
    catch (Exception ex)
    {
      throw new Exception($"authentication and/or b2c admin Guid Id retrieval failed, error m :\n {ex}");
    }

    var options = new SessionCreateOptions
    {
      SuccessUrl = "http://localhost:3000/",
      CancelUrl = "http://localhost:3000/leads",
      PaymentMethodTypes = new List<string>
                {
                    "card",
                },
      Mode = "subscription",
      LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = req.PriceId,
                        Quantity = 1,
                    },
                },
    };

    var service = new SessionService();
    service.Create(options);
    try
    {
      var session = await service.CreateAsync(options);
      var checkoutSessionResponse = new CheckoutSessionResponseDTO
      {
        SessionId = session.Id,
      };

       _repository.AddAsync(new CheckoutSession
      {
        BrokerId = b2cBrokerId,
        StripeCheckoutSessionId = session.Id
      });

      return Ok(checkoutSessionResponse);
    }
    catch (StripeException e)
    {
      Console.WriteLine(e.StripeError.Message);

      return BadRequest(e.StripeError.Message);
    }
  }

}
