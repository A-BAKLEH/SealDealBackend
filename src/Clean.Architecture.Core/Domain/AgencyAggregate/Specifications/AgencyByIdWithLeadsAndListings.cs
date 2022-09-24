
using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
public class AgencyByIdWithLeadsAndListings : Specification<Agency>, ISingleResultSpecification
{
  public AgencyByIdWithLeadsAndListings(int id)
  {
    Query.Where(a => a.Id == id).Include(x => x.Leads).Include(y => y.AgencyListings);
  }
}
