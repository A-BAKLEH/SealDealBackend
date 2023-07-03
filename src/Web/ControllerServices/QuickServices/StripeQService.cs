using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Web.Constants;

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
        if (GlobalControl.OurIds.Contains(brokerId))
        {
            var res = new List<dynamic>
            {
                new {Total = 69,
                    Currency = "testLol",
                    PeriodStart = DateTime.Now,
                    PeriodEnd = DateTime.Now,
                    Status = "paid",
                    PaidAt = DateTime.Now},
            };
            return res;
        }
        var broker = await dbContext.Brokers
            .Select(b => new { b.Agency.AdminStripeId, b.Id,b.Agency.StripeSubscriptionId })
            .FirstOrDefaultAsync(b => b.Id == brokerId);
        if (broker != null && !string.IsNullOrEmpty(broker.AdminStripeId))
        {
            var res = await _stripeBillingPortalService.GetCustomerInvoicesAsync(broker.AdminStripeId, broker.StripeSubscriptionId);
            return res;
        }
        return null;
    }
}
