namespace Clean.Architecture.Core.ServiceInterfaces.StripeInterfaces;
public interface IStripeService
{ 
  /// <summary>
  /// created a Checkout Session, saves its ID in Agency's LatestCheckoutSessionID property, and returns 
  /// the created session's ID
  /// </summary>
  /// <param name="priceID"></param>
  /// <param name="brokerID"></param>
  /// <param name="AgencyID"></param>
  /// <param name="Quantity"></param>
  /// <returns></returns>
  Task<string> CreateStripeCheckoutSessionAsync(string priceID, Guid brokerID, int AgencyID, int Quantity);

  /// <summary>
  /// Looks for the Agency the checkout session belongs to, if its StripeSubscription
  /// Status == NoStripeSubscription then assign its StripeSubscriptionId and AdminStripeID,
  /// and StripeSubscriptionStatus to CreatedWaitingForStatus
  /// </summary>
  /// <param name="CustomerId"></param>
  /// <param name="SubscriptionId"></param>
  /// <param name="sessionId"></param>
  /// <returns></returns>
  Task HandleCheckoutSessionCompletedAsync(string CustomerId, string SubscriptionId, string sessionId);

  /// <summary>
  /// retrieves 
  /// </summary>
  /// <returns></returns>
  Task HandleSubscriptionUpdatedAsync(string SubsID, string SubsStatus, long quanity, DateTime currPeriodEnd);
}
