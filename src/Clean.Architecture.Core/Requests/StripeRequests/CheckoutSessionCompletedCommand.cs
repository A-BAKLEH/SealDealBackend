

using Clean.Architecture.Core.Domain.AgencyAggregate.Specifications;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.ServiceInterfaces.StripeInterfaces;
using MediatR;
using Clean.Architecture.SharedKernel.Repositories;
using Clean.Architecture.SharedKernel.Exceptions;

namespace Clean.Architecture.Core.Requests.StripeRequests;
public class CheckoutSessionCompletedCommand : IRequest
{
  public string CustomerID { get; set; }
  public string SessionID { get; set; }
  public string SusbscriptionID { get; set; }
}

public class CheckoutSessionCompletedCommandHandler : IRequestHandler<CheckoutSessionCompletedCommand>
{
  private readonly IRepository<Agency> _agencyRepository;
  public CheckoutSessionCompletedCommandHandler(IRepository<Agency> agencyRepository)
  {
    _agencyRepository = agencyRepository;
  }

  public async Task<Unit> Handle(CheckoutSessionCompletedCommand request, CancellationToken cancellationToken)
  {
    var agency = await _agencyRepository.GetBySpecAsync(new AgencyByCheckoutSessionID(request.SessionID));
    if (agency == null) throw new InconsistentStateException("HandleCheckoutSessionCompletedCommand",$"Agency with sessionId {request.SessionID} not found");

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
