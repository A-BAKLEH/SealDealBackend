
using Ardalis.Specification;

namespace Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
public class AgencyByIdWithAreasAndLeads : Specification<Agency>, ISingleResultSpecification
{
  public AgencyByIdWithAreasAndLeads(int id)
  {
    Query.Where(a => a.Id == id).Include(a => a.Areas).Include(a => a.Leads);
  }

}
