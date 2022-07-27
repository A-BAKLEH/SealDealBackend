using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.Core.PaymentAggregate.Specification;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Clean.Architecture.Web.Api.WebhookController;
[Route("api/[controller]")]
[ApiController]
public class WebhookController : BaseApiController
{
  private readonly IRepository<CheckoutSession> _repository;

  public WebhookController(AuthorizeService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
  }

  [HttpPost("webhook")]
  public async Task<IActionResult> Webhook()
  {

    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    try
    {
      var stripeEvent = EventUtility.ConstructEvent(
       json,
       Request.Headers["Stripe-Signature"],
       "whsec_b400db1d64cf5beda49363cbb3bbc018c40d5612ddea70ba23cbff6e9738bb96"
       );

      // Handle the event
      if (stripeEvent.Type == Events.CheckoutSessionCompleted)
      {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

        addStripeIdToAgency(session.CustomerId, session.SubscriptionId, session.Id);

        Console.WriteLine(session.Id);
      }
      else if (stripeEvent.Type == Events.PaymentIntentSucceeded)
      {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
      }
      // ... handle other event types
      else
      {
        // Unexpected event type
        Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
      }
      return Ok();
    }
    catch (StripeException e)
    {
      Console.WriteLine(e.StripeError.Message);
      return BadRequest();
    }
  }

  private async void addStripeIdToAgency(string StripeAminId, string StripeSubsId, string stripecheckoutSessionID)
  {

    //Console.WriteLine(customer.DefaultSourceId);
    var checkoutSession = await _repository.GetBySpecAsync(new CheckoutSessionByIdWithBrokerAgencySpec(stripecheckoutSessionID));

    //checkoutSession.BrokerId
    var agency = checkoutSession.Broker.Agency;
    //var admin = _adminRepository.GetById(checkoutSession.Admin);
    //Broker.Agency;

    agency.AdminStripeId = StripeAminId;
    agency.IsPaying = true;
    agency.StripeSubscriptionId = StripeSubsId;
    //_agencyRepository.Update(agency);
    await _repository.UpdateAsync(checkoutSession);

  }
}
