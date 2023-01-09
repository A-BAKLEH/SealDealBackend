namespace SharedKernel.DomainEvents;
public class DomainEventBase : IDomainEvent
{
  public DomainEventBase()
  {
    this.OccurredOn = DateTime.Now;
  }

  public DateTime OccurredOn { get; }
}
