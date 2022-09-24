using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate.Specifications;
using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;

namespace Clean.Architecture.Core.MediatrRequests.StripeRequests;
public class CreateBillingPortalRequest : IRequest<string>
{
  public Guid BrokerId { get; set; }
  public string returnURL { get; set; }
}

public class CreateBillingPortalRequestHandler : IRequestHandler<CreateBillingPortalRequest, string>
{
  private readonly IStripeBillingPortalService _stripeService;
  private readonly IRepository<Broker> _repository;
  public CreateBillingPortalRequestHandler(IStripeBillingPortalService stripeService, IRepository<Broker> BrokerRepository)
  {
    _stripeService = stripeService;
    _repository = BrokerRepository;
  }

  public async Task<string> Handle(CreateBillingPortalRequest request, CancellationToken cancellationToken)
  {
    var Admin = await _repository.GetBySpecAsync(new BrokerByIdWithAgencySpec(request.BrokerId));
    var AgencyStripeID = Admin.Agency.AdminStripeId;
    var billingPortalURL = await _stripeService.CreateStripeBillingSessionAsync(AgencyStripeID, request.returnURL);
    return billingPortalURL;

  }
}
