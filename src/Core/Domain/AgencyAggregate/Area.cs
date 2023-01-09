
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.AgencyAggregate;

public class Area : Entity<int>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string Name { get; set; }
  public string PostalCode { get; set; }

  public List<Lead> InterestedLeads { get; set; }
}

