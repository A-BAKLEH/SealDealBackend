
using Clean.Architecture.Core.Interfaces.StripeInterfaces;
using MediatR;

namespace Clean.Architecture.Core.Commands_Handlers.StripeCommands;
public  class CheckoutSessionCommand : IRequest<string>
{
  public Guid adminId { get; set; }
  public string priceID { get; set; }
  public int AgencyID { get; set; }
  public int Quantity { get; set; }
}

public class CheckoutSessionCommandHandler : IRequestHandler<CheckoutSessionCommand, string>
{
  private readonly IStripeService _stripeService;

  public CheckoutSessionCommandHandler( IStripeService stripeService)
  {
    _stripeService = stripeService;
  }

  public async Task<string> Handle(CheckoutSessionCommand request, CancellationToken cancellationToken)
  {
    return await _stripeService.CreateStripeCheckoutSessionAsync(request.priceID, request.adminId, request.AgencyID, request.Quantity);
  }
}
