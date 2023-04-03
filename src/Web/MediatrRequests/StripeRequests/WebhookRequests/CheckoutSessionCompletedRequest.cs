
using Core.Domain.AgencyAggregate;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;

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
    public CheckoutSessionCompletedRequestHandler(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task Handle(CheckoutSessionCompletedRequest request, CancellationToken cancellationToken)
    {
        var agency = await _appDbContext.Agencies.OrderByDescending(a => a.Id).FirstOrDefaultAsync(a => a.LastCheckoutSessionID == request.SessionID);
        if (agency == null) throw new InconsistentStateException("HandleCheckoutSessionCompletedCommand", $"Agency with sessionId {request.SessionID} not found", "agency not found");

        if (agency.StripeSubscriptionStatus == StripeSubscriptionStatus.NoStripeSubscription)
        {
            agency.StripeSubscriptionId = request.SusbscriptionID;
            agency.AdminStripeId = request.CustomerID;
            agency.StripeSubscriptionStatus = StripeSubscriptionStatus.CreatedWaitingForStatus;
        }
        else
        {
            //TODO handle when there is already a StripeSubscription
        }
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
