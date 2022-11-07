﻿

using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel.Exceptions;
using Stripe;

namespace Clean.Architecture.Infrastructure.ExternalServices.Stripe;
internal class StripeSubscriptionService : IStripeSubscriptionService
{
  public async Task<int> AddSubscriptionQuantityAsync(string SubsId, int QuantityToAdd, int CurrentQuantity)
  {
    var service = new SubscriptionService();
    var stripeSubs = await service.GetAsync(SubsId);
    var quant = stripeSubs.Items.Data[0].Quantity;
    if (quant != CurrentQuantity) throw new InconsistentStateException("AddSubscriptionQuantity",$"DB Stripe Subs" +
      $" Quantity is {CurrentQuantity};Stripe actual quantity is {quant}", "Stripe Sync Error");

    var items = new List<SubscriptionItemOptions>
    {
        new SubscriptionItemOptions
        {
            Id = stripeSubs.Items.Data[0].Id,
            Quantity = quant + QuantityToAdd,
        },
    };

    var options = new SubscriptionUpdateOptions
    {
      Items = items,
      CancelAtPeriodEnd = false,
    };
    stripeSubs = await service.UpdateAsync(SubsId, options);
    return (int) stripeSubs.Items.Data[0].Quantity;

  }
}
