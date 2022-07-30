
using Clean.Architecture.Core.Interfaces.Stripe;
using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Clean.Architecture.Infrastructure.Services.Stripe;
public class StripeService : IStripeService
{
  private readonly IConfigurationSection _stripeConfigSection;
  private readonly IRepository<CheckoutSession> _checkoutSessionRepository;
  private string CheckoutSuccessURL;
  private string CheckoutCancelURL;
  public StripeService(IConfiguration config, IRepository<CheckoutSession> repository)
  {
    _stripeConfigSection = config.GetSection("StripeOptions");

    StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];
    CheckoutCancelURL = _stripeConfigSection.GetSection("SessionCreateOptions")["CancelUrl"];
    CheckoutSuccessURL = _stripeConfigSection.GetSection("SessionCreateOptions")["SuccessUrl"];
    _checkoutSessionRepository = repository;
  }
  public async Task<string> CreateStripeCheckoutSessionAsync(string priceID, Guid brokerID, int AgencyID, int Quantity = 1)
  {
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

    var service = new SessionService();
    var session = service.CreateAsync(options).Result;

    await _checkoutSessionRepository.AddAsync(new CheckoutSession
    {
      BrokerId = brokerID,
      AgencyId = AgencyID,
      StripeCheckoutSessionId = session.Id
    });

    return session.Id;
  }
}
