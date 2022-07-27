using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;

namespace Clean.Architecture.Core.PaymentAggregate.Specification;
public class CheckoutSessionByIdWithBrokerAgencySpec : Specification<CheckoutSession> , ISingleResultSpecification
{
  public CheckoutSessionByIdWithBrokerAgencySpec(string checkoutID)
  {
    Query.Where(x => x.StripeCheckoutSessionId == checkoutID)
          .Include(x => x.Broker)
          .ThenInclude(x => x.Agency);
  }
}
