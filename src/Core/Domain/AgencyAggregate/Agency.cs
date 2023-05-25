using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.AgencyAggregate;

public enum StripeSubscriptionStatus
{
    SubscriptionCancelled, Active, NoStripeSubscription, SubscriptionPaused, CreatedWaitingForStatus
}

public class Agency : Entity<int>
{
    public string AgencyName { get; set; }
    public Address? Address { get; set; }
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// client timeZ
    /// </summary>
    public DateTimeOffset SignupDateTime { get; set; } = DateTimeOffset.UtcNow;

    public string? AdminStripeId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    /// <summary>
    /// client timeZ
    /// </summary>
    public DateTimeOffset? SubscriptionLastValidDate { get; set; }
    public int NumberOfBrokersInSubscription { get; set; }
    public StripeSubscriptionStatus StripeSubscriptionStatus { get; set; }

    public bool HasAdminEmailConsent { get; set; } = false;
    public string? AzureTenantID { get; set; }
    /// <summary>
    /// Replace by separate entity and add/delete sessionIDs as they get created/processed,
    /// can be searched by index easily
    /// OR just use url parameters to include the id of the agency or admin
    /// </summary>
    public string? LastCheckoutSessionID { get; set; }
    public int NumberOfBrokersInDatabase { get; set; }

    public List<Listing> AgencyListings { get; set; }

    public List<Broker> AgencyBrokers { get; set; }

    public List<Area> Areas { get; set; }

    public List<Lead> Leads { get; set; }

}

