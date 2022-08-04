using Clean.Architecture.Core.Commands_Handlers.StripeCommands;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Clean.Architecture.Web.Api.WebhookController;

public class WebhookController : BaseApiController
{

  public WebhookController(AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
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

      //checkuot session completed is used only the first time a customer creates a subscription,
      //it only assigns Stripe susbID and CustomerID to an Agency in the Database and sets its
      // Subscription Status to CreatedWaiting for Status
      if (stripeEvent.Type == Events.CheckoutSessionCompleted)
      {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        await _mediator.Send(new CheckoutSessionCompletedCommand
        {
          SessionID = session.Id,
          SusbscriptionID = session.SubscriptionId,
          CustomerID = session.CustomerId,
        });
        Console.WriteLine(session.Id);
      }
      else if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
      {
        var subscription = stripeEvent.Data.Object as Subscription;
        Console.WriteLine("A subscription was updated.", subscription.Id);
        await _mediator.Send(new SubscriptionUpdatedCommand
        {
          SubscriptionId = subscription.Id,
          SubsStatus = subscription.Status,
          currPeriodEnd = subscription.CurrentPeriodEnd,
          quanity = subscription.Items.Data[0].Quantity
        });
      }
      else if (stripeEvent.Type == Events.InvoicePaymentFailed)
      {
        //TODO handle payment failure

      }
      else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
      {
        //TODO handle payment failure

      }
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
}
