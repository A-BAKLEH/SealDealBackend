
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Clean.Architecture.Infrastructure.ExternalServices.Stripe;
public class StripeCheckoutService : IStripeCheckoutService
{
  private readonly IConfigurationSection _stripeConfigSection;
  private string CheckoutSuccessURL;
  private string CheckoutCancelURL;
  public StripeCheckoutService(IConfiguration config)
  {
    _stripeConfigSection = config.GetSection("StripeOptions");

    StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];
    CheckoutCancelURL = _stripeConfigSection.GetSection("CheckoutSessionCreateOptions")["CancelUrl"];
    CheckoutSuccessURL = _stripeConfigSection.GetSection("CheckoutSessionCreateOptions")["SuccessUrl"];
  }

  public async Task<string> CreateStripeCheckoutSessionAsync(string priceID, int Quantity = 1)
  {
    var service = new SessionService();
    var options = new SessionCreateOptions
    {
      SuccessUrl = CheckoutSuccessURL,
      CancelUrl = CheckoutCancelURL,
      PaymentMethodTypes = new List<string>
                {
                    "card",
                },
      Mode = "subscription",
      LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceID,
                        Quantity = Quantity,
                    },
                },
    };
    var session = await service.CreateAsync(options);
    
    return session.Id;
  }
}
