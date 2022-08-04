using Clean.Architecture.Core.Interfaces.StripeInterfaces;
using MediatR;

namespace Clean.Architecture.Core.Commands_Handlers.StripeCommands;
public class SubscriptionUpdatedCommand : IRequest
{
  public string SubscriptionId { get; set; }
  public string SubsStatus { get; set; }
  public long quanity { get; set; }
  public DateTime currPeriodEnd { get; set; }
}

public class SubscriptionUpdatedCommandHandler : IRequestHandler<SubscriptionUpdatedCommand>
{
  private readonly IStripeService _stripeService;

  public SubscriptionUpdatedCommandHandler(IStripeService stripeService)
  {
    _stripeService = stripeService;
  }

  public async Task<Unit> Handle(SubscriptionUpdatedCommand request, CancellationToken cancellationToken)
  {
    await _stripeService.HandleSubscriptionUpdatedAsync(request.SubscriptionId, request.SubsStatus, request.quanity, request.currPeriodEnd);
    return Unit.Value;
  }
}
