using MediatR;

namespace Clean.Architecture.Core.Config;
public interface IDomainEventNotification<out TEventType> : IDomainEventNotification
{
  TEventType DomainEvent { get; }
}

public interface IDomainEventNotification : INotification
{
  //Guid Id { get; }
}
