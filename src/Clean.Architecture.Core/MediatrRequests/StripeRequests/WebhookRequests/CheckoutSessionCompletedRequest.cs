

using Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using MediatR;
using Clean.Architecture.SharedKernel.Repositories;
using Clean.Architecture.SharedKernel.Exceptions;

namespace Clean.Architecture.Core.MediatrRequests.StripeRequests.WebhookRequests;
public class CheckoutSessionCompletedRequest : IRequest
{
  public string CustomerID { get; set; }
  public string SessionID { get; set; }
  public string SusbscriptionID { get; set; }
}

public class CheckoutSessionCompletedRequestHandler : IRequestHandler<CheckoutSessionCompletedRequest>
{
  private readonly IRepository<Agency> _agencyRepository;
  public CheckoutSessionCompletedRequestHandler(IRepository<Agency> agencyRepository)
  {
    _agencyRepository = agencyRepository;
  }

  public async Task<Unit> Handle(CheckoutSessionCompletedRequest request, CancellationToken cancellationToken)
  {
    var agency = await _agencyRepository.GetBySpecAsync(new AgencyByCheckoutSessionID(request.SessionID));
    if (agency == null) throw new InconsistentStateException("HandleCheckoutSessionCompletedCommand", $"Agency with sessionId {request.SessionID} not found");

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
    await _agencyRepository.UpdateAsync(agency);
    return Unit.Value;
  }
}
