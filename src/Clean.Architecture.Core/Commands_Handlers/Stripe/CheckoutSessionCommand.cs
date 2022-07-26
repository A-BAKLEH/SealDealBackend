
using Clean.Architecture.Core.Interfaces.Stripe;
using Clean.Architecture.Core.PaymentAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using MediatR;

namespace Clean.Architecture.Core.Commands_Handlers.Stripe;
public  class CheckoutSessionCommand : IRequest<CheckoutSession>
{
  public Guid adminId { get; set; } 
}

public class CheckoutSessionCommandHandler : IRequestHandler<CheckoutSessionCommand, CheckoutSession>
{
  private readonly IRepository<CheckoutSession> _repository;
  private readonly IStripeService _stripeService;

  public CheckoutSessionCommandHandler(IRepository<CheckoutSession> repository, IStripeService stripeService)
  {
    _repository = repository;
    _stripeService = stripeService;
  }

  public async Task<CheckoutSession> Handle(CheckoutSessionCommand request, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
