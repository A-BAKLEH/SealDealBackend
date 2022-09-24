using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.LeadAggregate.Specifications;
public class LeadByIdWithListings : Specification<Lead>, ISingleResultSpecification
{
  public LeadByIdWithListings(int id)
  {
    Query.Where(l => l.Id == id).Include(l => l.ListingsOfInterest);
  }
}
