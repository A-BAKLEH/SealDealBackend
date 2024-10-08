﻿using Core.DTOs;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;

namespace Infrastructure.ExternalServices.Stripe;
public class StripeCheckoutService : IStripeCheckoutService
{
    private readonly IConfigurationSection _stripeConfigSection;
    private string CheckoutSuccessURL;
    private string CheckoutCancelURL;
    public StripeCheckoutService(IConfiguration config)
    {
        _stripeConfigSection = config.GetSection("StripeOptions");
        CheckoutCancelURL = _stripeConfigSection.GetSection("CheckoutSessionCreateOptions")["CancelUrl"];
        CheckoutSuccessURL = _stripeConfigSection.GetSection("CheckoutSessionCreateOptions")["SuccessUrl"];
    }

    public async Task<CheckoutSessionDTO> CreateStripeCheckoutSessionAsync(string priceID, int Quantity = 1)
    {
        var service = new SessionService();
        var options = new SessionCreateOptions
        {
            AllowPromotionCodes = true,
            SuccessUrl = CheckoutSuccessURL,
            CancelUrl = CheckoutCancelURL,
            PaymentMethodTypes = new List<string>
                {
                    "card",
                },
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceID,
                        Quantity = Quantity,
                    },
                },
        };
        var session = await service.CreateAsync(options);

        return new CheckoutSessionDTO { sessionId = session.Id, sessionURL = session.Url };
    }
}
