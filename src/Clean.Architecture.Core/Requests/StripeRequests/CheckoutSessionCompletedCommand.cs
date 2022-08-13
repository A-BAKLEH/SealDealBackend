

using Clean.Architecture.Core.ServiceInterfaces.StripeInterfaces;
using MediatR;

namespace Clean.Architecture.Core.Requests.StripeRequests;
public class CheckoutSessionCompletedCommand : IRequest
{
  public string CustomerID { get; set; }
  public string SessionID { get; set; }
  public string SusbscriptionID { get; set; }
}

public class CheckoutSessionCompletedCommandHandler : IRequestHandler<CheckoutSessionCompletedCommand>
{
  private readonly IStripeService _stripeService;

  public CheckoutSessionCompletedCommandHandler(IStripeService stripeService)
  {
    _stripeService = stripeService;
  }

  public async Task<Unit> Handle(CheckoutSessionCompletedCommand request, CancellationToken cancellationToken)
  {
    await _stripeService.HandleCheckoutSessionCompletedAsync(request.CustomerID, request.SusbscriptionID, request.SessionID);
    return Unit.Value;
  }
}
