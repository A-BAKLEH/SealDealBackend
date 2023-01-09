
using Core.Domain.AgencyAggregate;
using Core.DTOs;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using SharedKernel;
using MediatR;

namespace Web.MediatrRequests.StripeRequests;
public  class CreateCheckoutSessionRequest : IRequest<CheckoutSessionDTO>
{
  public string priceID { get; set; }
  public Agency agency { get; set; }
  public int Quantity { get; set; }
}

public class CreateCheckoutSessionRequestHandler : IRequestHandler<CreateCheckoutSessionRequest, CheckoutSessionDTO>
{
  private readonly IStripeCheckoutService _stripeService;
  private readonly AppDbContext _appDbContext;
  public CreateCheckoutSessionRequestHandler( IStripeCheckoutService stripeService,AppDbContext appDbContext)
  {
    _stripeService = stripeService;
    _appDbContext = appDbContext;
  }

  public async Task<CheckoutSessionDTO> Handle(CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
  {
    var checkoutSessionDTO = await _stripeService.CreateStripeCheckoutSessionAsync(request.priceID, request.Quantity);
    request.agency.LastCheckoutSessionID = checkoutSessionDTO.sessionId;
    await _appDbContext.SaveChangesAsync();
    return checkoutSessionDTO;
  }
}
