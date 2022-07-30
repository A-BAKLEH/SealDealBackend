

namespace Clean.Architecture.Core.Interfaces.Stripe;
public interface IStripeService
{ 
  Task<string> CreateStripeCheckoutSessionAsync(string priceID, Guid brokerID, int AgencyID, int Quantity);
}
