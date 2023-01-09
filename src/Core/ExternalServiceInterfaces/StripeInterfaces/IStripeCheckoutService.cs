using Core.DTOs;

namespace Core.ExternalServiceInterfaces.StripeInterfaces;
public interface IStripeCheckoutService
{ 
  /// <summary>
  /// creates a Checkout Session and returns its Id
  /// </summary>
  /// <param name="priceID"></param>
  /// <param name="brokerID"></param>
  /// <param name="AgencyID"></param>
  /// <param name="Quantity"></param>
  /// <returns>Created Checkout Session's Id</returns>
  Task<CheckoutSessionDTO> CreateStripeCheckoutSessionAsync(string priceID, int Quantity);
}
