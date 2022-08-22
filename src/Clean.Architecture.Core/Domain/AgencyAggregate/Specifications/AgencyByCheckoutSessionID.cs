
using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
public class AgencyByCheckoutSessionID : Specification<Agency>, ISingleResultSpecification
{
  public AgencyByCheckoutSessionID(string CheckoutSessionID)
  {
    Query.Where(a => a.LastCheckoutSessionID == CheckoutSessionID);
  }
}
