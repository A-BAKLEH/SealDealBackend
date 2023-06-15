using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.ControllerServices.QuickServices;

public class StripeQService
{
    private readonly IStripeBillingPortalService _stripeBillingPortalService;
    private readonly AppDbContext dbContext;
    public StripeQService(IStripeBillingPortalService stripeBillingPortalService, AppDbContext appDbContext)
    {
        _stripeBillingPortalService = stripeBillingPortalService;
        dbContext = appDbContext;
    }
    public async Task<dynamic> GetInvoicesAsync(Guid brokerId)
    {
        var broker = await dbContext.Brokers
            .Select(b => new { b.Agency.AdminStripeId, b.Id })
            .FirstOrDefaultAsync(b => b.Id == brokerId);
        if (broker != null && !string.IsNullOrEmpty(broker.AdminStripeId))
        {
            var res = await _stripeBillingPortalService.GetCustomerInvoicesAsync(broker.AdminStripeId);
            return res;
        }
        return null;
    }
}
