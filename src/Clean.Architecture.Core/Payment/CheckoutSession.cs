using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;

namespace Clean.Architecture.Core.Payment;
public class CheckoutSession : Entity<int>, IAggregateRoot
{
  public string StripeCheckoutSessionId { get; set; }

  public DateTime SessionStartAt { get; set; } = DateTime.UtcNow;

  public DateTime SessionEndAt { get; set; }

  public bool IsCompleted { get; set; }

  public Guid BrokerId { get; set; }
  public Broker Broker { get; set; }




}
