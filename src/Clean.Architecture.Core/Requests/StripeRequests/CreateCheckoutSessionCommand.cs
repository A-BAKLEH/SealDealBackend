
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.ServiceInterfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;

namespace Clean.Architecture.Core.Requests.StripeRequests;
public  class CreateCheckoutSessionCommand : IRequest<string>, ITransactional
{
  public string priceID { get; set; }
  public int AgencyID { get; set; }
  public int Quantity { get; set; }
}

public class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, string>
{
  private readonly IStripeService _stripeService;
  private readonly IRepository<Agency> _repository;
  public CreateCheckoutSessionCommandHandler( IStripeService stripeService,IRepository<Agency> AgencyRepository)
  {
    _stripeService = stripeService;
    _repository = AgencyRepository;
  }

  public async Task<string> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
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
