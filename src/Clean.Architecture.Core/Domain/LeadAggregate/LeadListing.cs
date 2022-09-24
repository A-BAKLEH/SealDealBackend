using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.LeadAggregate;
public class LeadListing : EntityBase
{
  public string? ClientComments { get; set; }
  public int LeadId { get; set; }
  public int ListingId { get; set; }

  public Lead Lead { get; set; }
  public Listing Listing { get; set; }
}
