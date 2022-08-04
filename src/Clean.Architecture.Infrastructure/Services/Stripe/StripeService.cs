using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.AgencyAggregate.Specifications;
using Clean.Architecture.Core.Interfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Clean.Architecture.Infrastructure.Services.Stripe;
public class StripeService : IStripeService
{
  private readonly IConfigurationSection _stripeConfigSection;
  private readonly IRepository<Agency> _agencyRepository;
  private string CheckoutSuccessURL;
  private string CheckoutCancelURL;
  public StripeService(IConfiguration config, IRepository<Agency> repository)
  {
    _stripeConfigSection = config.GetSection("StripeOptions");

    StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];
    CheckoutCancelURL = _stripeConfigSection.GetSection("SessionCreateOptions")["CancelUrl"];
    CheckoutSuccessURL = _stripeConfigSection.GetSection("SessionCreateOptions")["SuccessUrl"];
    _agencyRepository = repository;
  }
  //only creating the stripe session belongs here
  public async Task<string> CreateStripeCheckoutSessionAsync(string priceID, Guid brokerID, int AgencyID, int Quantity = 1)
  {
    var service = new SessionService();
    var agencyTask = _agencyRepository.GetByIdAsync(AgencyID);
    var options = new SessionCreateOptions
    {
      SuccessUrl = CheckoutSuccessURL,
      CancelUrl = CheckoutCancelURL,
      PaymentMethodTypes = new List<string>
                {
                    "card",
                },
      Mode = "subscription",
      LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceID,
                        Quantity = Quantity,
                    },
                },
    };
    var session = await service.CreateAsync(options);
    var agency = await agencyTask;
    agency.LastCheckoutSessionID = session.Id;
    await _agencyRepository.UpdateAsync(agency);

    return session.Id;
  }

  //doesnt belong here
  public async Task HandleCheckoutSessionCompletedAsync(string CustomerId, string SubscriptionId, string sessionId)
  {
    var agency = await _agencyRepository.GetBySpecAsync(new AgencyByCheckoutSessionID(sessionId));
    if (agency == null) throw new Exception("HandleCheckoutSessionCompletedAsync: agency not found");
    
    if(agency.StripeSubscriptionStatus == StripeSubscriptionStatus.NoStripeSubscription)
    {
      agency.StripeSubscriptionId = SubscriptionId;
      agency.AdminStripeId = CustomerId;
      agency.StripeSubscriptionStatus = StripeSubscriptionStatus.CreatedWaitingForStatus;
    }
    await _agencyRepository.UpdateAsync(agency);
  }
  public async Task HandleSubscriptionUpdatedAsync( string SubsID,string SubsStatus, long quanity, DateTime currPeriodEnd  )
  {
    var spec = new AgencyBySubsIDWithBrokers(SubsID);
    var agency = await _agencyRepository.GetBySpecAsync(spec);
    //CheckoutSessionCompleted event not fully processed yet
    if (agency == null)
    {
      Thread.Sleep(1500);
      agency = await _agencyRepository.GetBySpecAsync(spec);
      if (agency == null) throw new Exception($"no agency has this subscriptionID {SubsID} OR" +
        $"checkoutSessionCompleted not handled properly");
    }
    //Subs just created
    if (agency.StripeSubscriptionStatus == StripeSubscriptionStatus.CreatedWaitingForStatus)
    {
      if (SubsStatus == "active") agency.StripeSubscriptionStatus = StripeSubscriptionStatus.Active;
      //TODO: create DomainEvent On Agency when subscription status changes: it will maybe send an email and 
      //check numberofBrokers, enable broker accounts, etc
      foreach (var broker in agency.AgencyBrokers)
      {
        broker.AccountActive = true;
      }
      agency.NumberOfBrokersInSubscription = (int) quanity;
      agency.SubscriptionLastValidDate = currPeriodEnd;
      await _agencyRepository.UpdateAsync(agency);
    }
    //TODO: handle other possible cases
  }
}
