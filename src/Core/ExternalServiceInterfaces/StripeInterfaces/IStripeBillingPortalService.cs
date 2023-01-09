namespace Core.ExternalServiceInterfaces.StripeInterfaces;
public interface IStripeBillingPortalService
{
  /// <summary>
  /// creates stripe billing portal session and returns its URL
  /// </summary>
  /// <param name="priceID"></param>
  /// <param name="Quantity"></param>
  /// <returns>Created billing portal's URL</returns>
  Task<string> CreateStripeBillingSessionAsync(string AdminStripeId, string returnURL);
}
