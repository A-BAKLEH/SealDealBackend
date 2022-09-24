

using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Stripe.BillingPortal;

namespace Clean.Architecture.Infrastructure.ExternalServices.Stripe;
public class StripeBillingPortalService : IStripeBillingPortalService
{
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
