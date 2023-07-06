
using Core.Domain.AgencyAggregate;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using SharedKernel.Exceptions;
using Stripe;

namespace Web.MediatrRequests.StripeRequests.WebhookRequests;
public class CheckoutSessionCompletedRequest : IRequest
{
    public string CustomerID { get; set; }
    public string SessionID { get; set; }
    public string SusbscriptionID { get; set; }
}

public class CheckoutSessionCompletedRequestHandler : IRequestHandler<CheckoutSessionCompletedRequest>
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<CheckoutSessionCompletedRequestHandler> _logger;
    public CheckoutSessionCompletedRequestHandler(AppDbContext appDbContext, ILogger<CheckoutSessionCompletedRequestHandler> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task Handle(CheckoutSessionCompletedRequest request, CancellationToken cancellationToken)
    {
        var agency = await _appDbContext.Agencies.OrderByDescending(a => a.Id).FirstOrDefaultAsync(a => a.LastCheckoutSessionID == request.SessionID);
        if (agency == null) throw new InconsistentStateException("HandleCheckoutSessionCompletedCommand", $"Agency with sessionId {request.SessionID} not found", "agency not found");

        if (agency.StripeSubscriptionStatus == StripeSubscriptionStatus.NoStripeSubscription)
        {
            agency.StripeSubscriptionId = request.SusbscriptionID;
            agency.AdminStripeId = request.CustomerID;
            //agency.StripeSubscriptionStatus = StripeSubscriptionStatus.CreatedWaitingForStatus;

            //handle subscribtion
            var service = new SubscriptionService();
            var Subs = await service.GetAsync(request.SusbscriptionID);

            if (Subs.Status == "active") agency.StripeSubscriptionStatus = StripeSubscriptionStatus.Active;
            else if (Subs.Status == "trialing") agency.StripeSubscriptionStatus = StripeSubscriptionStatus.Trial;
            else _logger.LogWarning("{tag} not active or trial subsId {susbId}","checkoutSessionCompleted",Subs.Id);

            var admin = await _appDbContext.Brokers.FirstAsync(b => b.AgencyId == agency.Id && b.isAdmin);
            if(Subs.Status == "active" || Subs.Status == "trialing") admin.AccountActive = true;
            agency.NumberOfBrokersInSubscription = (int) Subs.Items.Data[0].Quantity;
            agency.SubscriptionLastValidDate = Subs.CurrentPeriodEnd;
            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            //TODO handle when there is already a StripeSubscription
        }
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
