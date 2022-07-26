
using Clean.Architecture.Core.PaymentAggregate;

namespace Clean.Architecture.Core.Interfaces.Stripe;
public interface IStripeService
{
  Task<CheckoutSession> CreateStripeCheckoutSessionAsync();
}
