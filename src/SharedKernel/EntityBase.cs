
namespace SharedKernel;
public abstract class EntityBase
{

  /*private List<DomainEventBase> _domainEvents = new ();
  [NotMapped]
  public IEnumerable<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

  protected void RegisterDomainEvent(DomainEventBase domainEvent) => _domainEvents.Add(domainEvent);
  internal void ClearDomainEvents() => _domainEvents.Clear();*/
  private List<IDomainEvent> _domainEvents;

  /// <summary>
  /// Domain events occurred.
  /// </summary>
  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly();

  /// <summary>
  /// Add domain event.
  /// </summary>
  /// <param name="domainEvent"></param>
  public void AddDomainEvent(IDomainEvent domainEvent)
  {
    _domainEvents = _domainEvents ?? new List<IDomainEvent>();
    this._domainEvents.Add(domainEvent);
  }

  /// <summary>
  /// Clear domain events.
  /// </summary>
  public void ClearDomainEvents()
  {
    _domainEvents?.Clear();
  }
}
