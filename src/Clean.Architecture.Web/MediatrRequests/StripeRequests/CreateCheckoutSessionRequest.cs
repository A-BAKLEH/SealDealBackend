
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;

namespace Clean.Architecture.Web.MediatrRequests.StripeRequests;
public  class CreateCheckoutSessionRequest : IRequest<string>, ITransactional
{
  public string priceID { get; set; }
  public int AgencyID { get; set; }
  public int Quantity { get; set; }
}

public class CreateCheckoutSessionRequestHandler : IRequestHandler<CreateCheckoutSessionRequest, string>
{
  private readonly IStripeCheckoutService _stripeService;
  private readonly IRepository<Agency> _repository;
  public CreateCheckoutSessionRequestHandler( IStripeCheckoutService stripeService,IRepository<Agency> AgencyRepository)
  {
    _stripeService = stripeService;
    _repository = AgencyRepository;
  }

  public async Task<string> Handle(CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
  {
    var CheckoutSessionIdTask = _stripeService.CreateStripeCheckoutSessionAsync(request.priceID, request.Quantity);
    var agencyTask = _repository.GetByIdAsync(request.AgencyID);
    var CheckoutSessionId = await CheckoutSessionIdTask;
    var agency = await agencyTask;
    agency.LastCheckoutSessionID = CheckoutSessionId;
    await _repository.UpdateAsync(agency);
    return CheckoutSessionId;
  }
}
