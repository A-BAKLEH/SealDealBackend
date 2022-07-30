using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;
namespace Clean.Architecture.Core.AgencyAggregate;

public enum AgencyStatus { 
  SubscriptionCancelled, isPaying, JustSignedUp, SubscriptionPaused,
}

public class Agency : Entity<int> , IAggregateRoot
{ 
  public string AgencyName { get; set; }

  public DateTime SignupDateTime { get; set; } = DateTime.UtcNow;
  public string? AdminStripeId { get; set; }

  public string? StripeSubscriptionId { get; set; }

  public Boolean SoloBroker { get; set; }

  public int NumberOfBrokersInSubscription { get; set; }

  public AgencyStatus AgencyStatus { get; set; }

  public List<Listing> AgencyListings { get; set; }

  public List<Broker> AgencyBrokers { get; set; }

  public List<Area> Areas { get; set; }

  public List<Lead> Leads { get; set; }

  public List<CheckoutSession> CheckoutSessions { get; set; }


}

