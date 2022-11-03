

using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.BillingPortal;

namespace Clean.Architecture.Infrastructure.ExternalServices.Stripe;
public class StripeBillingPortalService : IStripeBillingPortalService
{
  //api key initialized in Container
  //private readonly IConfigurationSection _stripeConfigSection;
  public StripeBillingPortalService(IConfiguration config)
  {
    //_stripeConfigSection = config.GetSection("StripeOptions");

    //StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];
  }
  public async Task<string> CreateStripeBillingSessionAsync(string AdminStripeId, string returnURL)
  {
    var options = new SessionCreateOptions
    {
      Customer = AdminStripeId,
      ReturnUrl = returnURL
    };

    var service = new SessionService();
    var session = await service.CreateAsync(options);
    return session.Url;
  }
}
