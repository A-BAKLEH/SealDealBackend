
using Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using MediatR;
using Clean.Architecture.SharedKernel.Repositories;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.StripeRequests.WebhookRequests;
public class SubscriptionUpdatedRequest : IRequest
{
  public string SubscriptionId { get; set; }
  public string SubsStatus { get; set; }
  public long quantity { get; set; }
  public DateTime currPeriodEnd { get; set; }
}

public class SubscriptionUpdatedRequestHandler : IRequestHandler<SubscriptionUpdatedRequest>
{
  private readonly AppDbContext _appDbContext;

  public SubscriptionUpdatedRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<Unit> Handle(SubscriptionUpdatedRequest request, CancellationToken cancellationToken)
  {
    var spec = new AgencyBySubsIDWithBrokers(request.SubscriptionId);
    Agency? agency = null;
    //CheckoutSessionCompleted event might not be fully processed yet

    for (var i = 0; i < 3; i++)
    {
      agency = await _appDbContext.Agencies.Include(a => a.AgencyBrokers.Take(1)).FirstOrDefaultAsync(a => a.StripeSubscriptionId == request.SubscriptionId);
      if (agency != null) break;
      else if (i < 2)
      {
        Thread.Sleep(800);
      }
      //add HangfireRetry
      else throw new InconsistentStateException("subscriptionUpdated-NoAgencyWithSubscriptionID", $"No agency found with subscriptionId {request.SubscriptionId}, new status is {request.SubsStatus}");
    }
    //Subs just created with CheckoutSessionCompleted Event
    if (agency.StripeSubscriptionStatus == StripeSubscriptionStatus.CreatedWaitingForStatus)
    {
      if (request.SubsStatus == "active") agency.StripeSubscriptionStatus = StripeSubscriptionStatus.Active;
      //TODO: create DomainEvent On Agency when subscription status changes: it will maybe send an email and 
      //check numberofBrokers, enable broker accounts, etc
      var admin = agency.AgencyBrokers.Single();
      admin.AccountActive = true;
      agency.NumberOfBrokersInSubscription = (int)request.quantity;
      agency.SubscriptionLastValidDate = request.currPeriodEnd;
      await _appDbContext.SaveChangesAsync();
    }
    else
    {
      //TODO handle other cases
    }
    return Unit.Value;
  }
}
