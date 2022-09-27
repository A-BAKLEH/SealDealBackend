using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.MediatrRequests.StripeRequests.WebhookRequests;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Clean.Architecture.Web.Api.StripeController;

public class StripeWebhookController : BaseApiController
{
  private readonly ILogger<StripeWebhookController> _logger;
  public StripeWebhookController(AuthorizationService authorizeService, IMediator mediator,ILogger<StripeWebhookController> logger) : base(authorizeService, mediator)
  {
    _logger = logger;
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
      //Subscription Status to CreatedWaiting for Status
      if (stripeEvent.Type == Events.CheckoutSessionCompleted)
      {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        _logger.BeginScope("[{Tag}] received {WebhookEvent} event with CheckoutSessionId {CheckoutSessionId}" +
          " and SubscriptionId {SubscriptionId}", TagConstants.Webhook, "CheckoutSessionCompleted",session.Id,session.SubscriptionId);

        await _mediator.Send(new CheckoutSessionCompletedRequest
        {
          SessionID = session.Id,
          SusbscriptionID = session.SubscriptionId,
          CustomerID = session.CustomerId,
        });
      }
      else if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
      {
        var subscription = stripeEvent.Data.Object as Subscription;
        _logger.BeginScope("[{Tag}] received {WebhookEvent} event with " +
          "SubscriptionId {SubscriptionId} and new Subscription Status {SubscriptionStatus}", TagConstants.Webhook, "SubscriptionUpdated", subscription.Id, subscription.Status);
        await _mediator.Send(new SubscriptionUpdatedRequest
        {
          SubscriptionId = subscription.Id,
          SubsStatus = subscription.Status,
          currPeriodEnd = subscription.CurrentPeriodEnd,
          quantity = subscription.Items.Data[0].Quantity
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
        _logger.LogError("[{Tag}] Unhandled {WebhookEvent} event", TagConstants.Webhook,stripeEvent.Type);
      }
      return Ok();
    }
    catch (StripeException e)
    {
      Console.WriteLine(e.StripeError.Message);
      return BadRequest();
    }
    catch(InconsistentStateException ex)
    {
      _logger.LogError("[{Tag}] InconsistentStateException with Tag {InconsistentStateExcTag} and Details {InconsistentStateExcDetails} encountered", TagConstants.Webhook, ex.tag, ex.details);
      return BadRequest();
    }
  }
}
