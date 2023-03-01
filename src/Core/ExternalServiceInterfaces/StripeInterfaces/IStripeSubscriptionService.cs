namespace Core.ExternalServiceInterfaces.StripeInterfaces;
public interface IStripeSubscriptionService
{
  /// <summary>
  /// Adds QuantityToAdd quantity to the current quantity in the subscription and returns the new 
  /// total quantity
  /// </summary>
  /// <param name="SubsId"></param>
  /// <param name="QuantityToAdd"></param>
  /// <returns></returns>
  Task<int> AddSubscriptionQuantityAsync(string SubsId, int QuantityToAdd, int CurrentQuantity);
  Task<int> DecreaseSubscriptionQuantityAsync(string SubsId,int CurrentQuantity);
}
