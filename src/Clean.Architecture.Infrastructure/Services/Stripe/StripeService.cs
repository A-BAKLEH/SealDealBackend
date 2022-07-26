

using Clean.Architecture.Core.Interfaces.Stripe;
using Clean.Architecture.Core.PaymentAggregate;

namespace Clean.Architecture.Infrastructure.Services.Stripe;
public class StripeService : IStripeService
{
  public Task<CheckoutSession> CreateStripeCheckoutSessionAsync()
  {
    throw new NotImplementedException();
  }

}
