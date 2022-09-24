
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;

namespace Clean.Architecture.Core.Domain.AgencyAggregate;

public class Area : Entity<int>, IAggregateRoot
{
  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string Name { get; set; }
  public string PostalCode { get; set; }

  public List<Lead> InterestedLeads { get; set; }
}

