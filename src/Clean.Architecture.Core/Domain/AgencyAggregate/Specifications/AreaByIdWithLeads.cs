
using Ardalis.Specification;
using Clean.Architecture.Core.Domain.LeadAggregate;

namespace Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
public class AreaByIdWithLeads : Specification<Area>, ISingleResultSpecification
{
  public AreaByIdWithLeads(int id)
  {
    Query.Where(area => area.Id == id).Include(a => a.InterestedLeads);
  }
}
