using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Exceptions;
using Stripe;
using Web.ControllerServices;
using Web.MediatrRequests.StripeRequests.WebhookRequests;

namespace Web.Api.StripeController;

public class StripeWebhookController : BaseApiController
{
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly string signature;
    public StripeWebhookController(AuthorizationService authorizeService,
        IMediator mediator,
        IConfiguration config,
        ILogger<StripeWebhookController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
        signature = config.GetSection("StripeOptions")["SignatureKey"];
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent
                (
                    json,
                    Request.Headers["Stripe-Signature"],
                    signature
                );
            //checkuot session completed is used only the first time a customer creates a subscription,
            //it only assigns Stripe susbID and CustomerID to an Agency in the Database and sets its
            //Subscription Status to CreatedWaiting for Status
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                _logger.LogInformation("{tag} received {webhookEvent} event with CheckoutSessionId {checkoutSessionId}" +
                  " and SubscriptionId {subscriptionId}", TagConstants.Webhook, "CheckoutSessionCompleted", session.Id, session.SubscriptionId);

                await _mediator.Send(new CheckoutSessionCompletedRequest
                {
                    SessionID = session.Id,
                    SusbscriptionID = session.SubscriptionId,
                    CustomerID = session.CustomerId,
                });
            }
            else if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
            {
            //    var subscription = stripeEvent.Data.Object as Subscription;
            //    _logger.LogInformation("{tag} received {webhookEvent} event with " +
            //      "SubscriptionId {subscriptionId} and new Subscription Status {subscriptionStatus}", TagConstants.Webhook, "SubscriptionUpdated", subscription.Id, subscription.Status);
            //    await _mediator.Send(new SubscriptionUpdatedRequest
            //    {
            //        SubscriptionId = subscription.Id,
            //        SubsStatus = subscription.Status,
            //        currPeriodEnd = subscription.CurrentPeriodEnd,
            //        quantity = subscription.Items.Data[0].Quantity
            //    });
            }
            else
            {
                // Unexpected event type
                _logger.LogWarning("{tag} Unhandled {webhookEvent} event", TagConstants.Webhook, stripeEvent.Type);
            }
            return Ok();
        }
        catch (StripeException e)
        {
            Console.WriteLine(e.StripeError.Message);
            return BadRequest();
        }
        catch (InconsistentStateException ex)
        {
            _logger.LogError("{tag} InconsistentStateException with Tag {inconsistentStateExcTag} and Details {inconsistentStateExcDetails} encountered", TagConstants.Webhook, ex.tag, ex.details);
            return BadRequest();
        }
    }
}
