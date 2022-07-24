using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
namespace Clean.Architecture.Core.AgencyAggregate;

public class Area : Entity<int>
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string Name { get; set; }
  public int PostalCode { get; set; }

  public List<Lead> InterestedLeads { get; set; }
}

