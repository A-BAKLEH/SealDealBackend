using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;

namespace Clean.Architecture.Core.PaymentAggregate;

public enum CheckoutSessionStatus
{
  Open, Complete, Expired
}
public class CheckoutSession : Entity<int>, IAggregateRoot
{
  public string StripeCheckoutSessionId { get; set; }

  public DateTime SessionStartAt { get; set; } = DateTime.UtcNow;

  public DateTime SessionEndAt { get; set; }

  public CheckoutSessionStatus CheckoutSessionStatus { get; set; } = CheckoutSessionStatus.Open;

  //stripe "status" enum : open: in progress with no payment processing started, complete: completed but payment
  //processing may still be in progress, expired

  //stripe "payment_status" enum : paid : funds available, unpaid : funds not yet available , no payment required
  public Guid? BrokerId { get; set; }
  public Broker Broker { get; set; }

  public Agency Agency { get; set; }
  public int AgencyId { get; set; }

}
