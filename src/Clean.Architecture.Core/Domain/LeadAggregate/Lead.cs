
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;

namespace Clean.Architecture.Core.Domain.LeadAggregate;

public enum Status
{
  New, Active, Client, Closed, Dead
}

public class Lead : Entity<int>, IAggregateRoot
{

  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string LeadFirstName { get; set; }
  public int LeadLastName { get; set; }
  public string PhoneNumber { get; set; }
  public string Email { get; set; }
  public int Budget { get; set; }
  public DateTime EntryDate { get; set; } = DateTime.UtcNow;
  public List<History> Histories { get; set; }
  public Status LeadStatus { get; set; } = Status.New;
  public Broker Broker { get; set; }
  public Guid? BrokerId { get; set; }

  public List<Area> AreasOfInterest { get; set; }

  public List<Listing> ListingOfInterest { get; set; }

  public List<Note> Notes { get; set; }
  public List<Tag> Tags { get; set; }

}

