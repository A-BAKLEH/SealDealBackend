
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.StripeRequests;
public  class CreateCheckoutSessionRequest : IRequest<string>, ITransactional
{
  public string priceID { get; set; }
  public Agency agency { get; set; }
  public int Quantity { get; set; }
}

public class CreateCheckoutSessionRequestHandler : IRequestHandler<CreateCheckoutSessionRequest, string>
{
  private readonly IStripeCheckoutService _stripeService;
  private readonly AppDbContext _appDbContext;
  public CreateCheckoutSessionRequestHandler( IStripeCheckoutService stripeService,AppDbContext appDbContext)
  {
    _stripeService = stripeService;
    _appDbContext = appDbContext;
  }

  public async Task<string> Handle(CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
  {
    var CheckoutSessionId = await _stripeService.CreateStripeCheckoutSessionAsync(request.priceID, request.Quantity);
    request.agency.LastCheckoutSessionID = CheckoutSessionId;
    await _appDbContext.SaveChangesAsync();
    return CheckoutSessionId;
  }
}
