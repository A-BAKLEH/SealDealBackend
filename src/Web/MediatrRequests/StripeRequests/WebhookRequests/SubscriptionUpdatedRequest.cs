﻿
using Core.Domain.AgencyAggregate;
using MediatR;
using SharedKernel.Exceptions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.MediatrRequests.StripeRequests.WebhookRequests;
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

  public async Task Handle(SubscriptionUpdatedRequest request, CancellationToken cancellationToken)
  {
    Agency? agency = null;
    //CheckoutSessionCompleted event might not be fully processed yet

    for (var i = 0; i < 3; i++)
    {
      agency = await _appDbContext.Agencies.Include(a => a.AgencyBrokers).FirstOrDefaultAsync(a => a.StripeSubscriptionId == request.SubscriptionId);
      if (agency != null) break;
      else if (i < 2)
      {
        await Task.Delay(800);
      }
      //add HangfireRetry
      else throw new InconsistentStateException("subscriptionUpdated-NoAgencyWithSubscriptionID", $"No agency found with subscriptionId {request.SubscriptionId}, new status is {request.SubsStatus}","No Matching Agency");
    }
    //Subs just created with CheckoutSessionCompleted Event
    if (agency.StripeSubscriptionStatus == StripeSubscriptionStatus.CreatedWaitingForStatus)
    {
      if (request.SubsStatus == "active") agency.StripeSubscriptionStatus = StripeSubscriptionStatus.Active;
      //TODO: create DomainEvent On Agency when subscription status changes: it will maybe send an email and 
      //check numberofBrokers, enable broker accounts, etc
      var admin = agency.AgencyBrokers.First(b => b.isAdmin);
      admin.AccountActive = true;
      agency.NumberOfBrokersInSubscription = (int)request.quantity;
      agency.SubscriptionLastValidDate = request.currPeriodEnd;
      await _appDbContext.SaveChangesAsync();
    }
    else
    {
      //TODO handle other cases
    }
  }
}
