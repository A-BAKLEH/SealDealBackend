namespace SharedKernel.DomainEvents;
public class DomainEventBase : IDomainEvent
{
  public DomainEventBase()
  {
    this.OccurredOn = DateTime.UtcNow;
  }

  public DateTime OccurredOn { get; }
}
