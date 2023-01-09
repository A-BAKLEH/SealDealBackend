
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using MediatR;

namespace Web.MediatrRequests.StripeRequests;
public class CreateBillingPortalRequest : IRequest<string>
{
  public string AgencyStripeId { get; set; }
  public string returnURL { get; set; }
}

public class CreateBillingPortalRequestHandler : IRequestHandler<CreateBillingPortalRequest, string>
{
  private readonly IStripeBillingPortalService _stripeService;
  public CreateBillingPortalRequestHandler(IStripeBillingPortalService stripeService)
  {
    _stripeService = stripeService;
  }

  public async Task<string> Handle(CreateBillingPortalRequest request, CancellationToken cancellationToken)
  {
    var billingPortalURL = await _stripeService.CreateStripeBillingSessionAsync(request.AgencyStripeId, request.returnURL);
    return billingPortalURL;
  }
}
