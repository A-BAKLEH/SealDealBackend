

using Core.ExternalServiceInterfaces.StripeInterfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.BillingPortal;

namespace Infrastructure.ExternalServices.Stripe;
public class StripeBillingPortalService : IStripeBillingPortalService
{
    //api key initialized in Container
    //private readonly IConfigurationSection _stripeConfigSection;
    public StripeBillingPortalService(IConfiguration config)
    {
        //_stripeConfigSection = config.GetSection("StripeOptions");

        //StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];
    }
    public async Task<string> CreateStripeBillingSessionAsync(string AdminStripeId, string returnURL)
    {
        var options = new SessionCreateOptions
        {
            Customer = AdminStripeId,
            ReturnUrl = returnURL
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<dynamic> GetCustomerInvoicesAsync(string stripeCustomerId)
    {
        var options = new InvoiceListOptions
        {
            Limit = 100,
            Customer = stripeCustomerId
        };
        var service = new InvoiceService();
        StripeList<Invoice> invoices = await service.ListAsync(
          options);
        var invList = invoices.Data;
        var response = invList.Select(i => new
        {
            i.Total,
            i.Currency,
            i.PeriodStart,
            i.PeriodEnd,
            i.Status,//draft, open, paid
            i.StatusTransitions.PaidAt,
        });
        return response;
    }
}
