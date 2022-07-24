using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;
namespace Clean.Architecture.Core.AgencyAggregate;
public class Agency : Entity<int> , IAggregateRoot
{ 
  public string AgencyName { get; set; }

  public DateTime SignupDateTime { get; set; } = DateTime.UtcNow;
  public Boolean IsPaying { get; set; }
  public string? AdminStripeId { get; set; }

  public string? StripeSubscriptionId { get; set; }

  public Boolean SoloBroker { get; set; }

  public List<Listing> AgencyListings { get; set; }

  public List<Broker> AgencyBrokers { get; set; }

  public List<Area> Areas { get; set; }

  public List<Lead> Leads { get; set; }


}

