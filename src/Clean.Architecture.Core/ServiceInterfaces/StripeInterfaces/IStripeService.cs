namespace Clean.Architecture.Core.ServiceInterfaces.StripeInterfaces;
public interface IStripeService
{ 
  /// <summary>
  /// creates a Checkout Session saves its ID in Agency's LatestCheckoutSessionID property, and returns 
  /// the created session's ID
  /// </summary>
  /// <param name="priceID"></param>
  /// <param name="brokerID"></param>
  /// <param name="AgencyID"></param>
  /// <param name="Quantity"></param>
  /// <returns>Created Checkout Session's Id</returns>
  Task<string> CreateStripeCheckoutSessionAsync(string priceID, int Quantity);
  Task<string> CreateBillingSessionAsync(string priceID, int Quantity);
}
