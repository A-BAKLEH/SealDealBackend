using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.Domain.AgencyAggregate;

public enum StripeSubscriptionStatus { 
  SubscriptionCancelled, Active, NoStripeSubscription, SubscriptionPaused, CreatedWaitingForStatus
}

public class Agency : Entity<int>
{ 
  public string AgencyName { get; set; }
  public Address? Address { get; set; }
  public string PhoneNumber { get; set; }
  public DateTime SignupDateTime { get; set; } = DateTime.UtcNow;

  public string? AdminStripeId { get; set; }
  public string? StripeSubscriptionId { get; set; }
  public DateTime? SubscriptionLastValidDate { get; set; }
  public int NumberOfBrokersInSubscription { get; set; }
  public StripeSubscriptionStatus StripeSubscriptionStatus { get; set; }

  public bool HasAdminEmailConsent { get; set; } = false;
  public string? AzureTenantID { get; set; }
  public string? LastCheckoutSessionID { get; set; }
  public int NumberOfBrokersInDatabase { get; set; }
  
  public List<Listing> AgencyListings { get; set; }

  public List<Broker> AgencyBrokers { get; set; }

  public List<Area> Areas { get; set; }

  public List<Lead> Leads { get; set; }

}

