
using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.LeadAggregate.Specifications;
public class LeadByIdWithAreas : Specification<Lead>, ISingleResultSpecification
{
  public LeadByIdWithAreas(int id)
  {
    Query.Where(l => l.Id == id).Include(l => l.AreasOfInterest);
  }
}
