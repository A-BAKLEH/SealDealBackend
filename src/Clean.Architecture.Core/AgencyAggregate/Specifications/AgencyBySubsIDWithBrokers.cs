﻿
using Ardalis.Specification;

namespace Clean.Architecture.Core.AgencyAggregate.Specifications;
public class AgencyBySubsIDWithBrokers : Specification<Agency>, ISingleResultSpecification
{
  public AgencyBySubsIDWithBrokers(string StripeSubsID)
  {
    Query.Where(a => a.StripeSubscriptionId == StripeSubsID).Include(x => x.AgencyBrokers);
  }
}
