using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;
namespace Clean.Architecture.Core.AgencyAggregate;

public enum StripeSubscriptionStatus { 
  SubscriptionCancelled, Active, NoStripeSubscription, SubscriptionPaused, CreatedWaitingForStatus
}

public class Agency : Entity<int> , IAggregateRoot
{ 
  public string AgencyName { get; set; }

  public DateTime SignupDateTime { get; set; } = DateTime.UtcNow;
  public string? AdminStripeId { get; set; }

  public string? StripeSubscriptionId { get; set; }

  public string? LastCheckoutSessionID { get; set; }
  public DateTime? SubscriptionLastValidDate { get; set; }
  public int NumberOfBrokersInSubscription { get; set; }

  public int NumberOfBrokersInDatabase { get; set; }
  public StripeSubscriptionStatus StripeSubscriptionStatus { get; set; }

  public List<Listing> AgencyListings { get; set; }

  public List<Broker> AgencyBrokers { get; set; }

  public List<Area> Areas { get; set; }

  public List<Lead> Leads { get; set; }

}

