
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
using Clean.Architecture.Core.ServiceInterfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel.Repositories;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Clean.Architecture.Infrastructure.ExternalServices.Stripe;
public class StripeService : IStripeService
{
  private readonly IConfigurationSection _stripeConfigSection;
  private readonly IRepository<Agency> _agencyRepository;
  private string CheckoutSuccessURL;
  private string CheckoutCancelURL;
  public StripeService(IConfiguration config, IRepository<Agency> repository)
  {
    _stripeConfigSection = config.GetSection("StripeOptions");

    StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];
    CheckoutCancelURL = _stripeConfigSection.GetSection("SessionCreateOptions")["CancelUrl"];
    CheckoutSuccessURL = _stripeConfigSection.GetSection("SessionCreateOptions")["SuccessUrl"];
    _agencyRepository = repository;
  }

  public Task<string> CreateBillingSessionAsync(string priceID, int Quantity)
  {
    throw new NotImplementedException();
  }

  //only creating the stripe session belongs here
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
