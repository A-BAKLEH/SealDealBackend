
using MediatR;

namespace Clean.Architecture.SharedKernel.DomainNotifications;
public interface IDomainEventNotification<out TEventType> : IDomainEventNotification
{
  TEventType DomainEvent { get; }
}

public interface IDomainEventNotification : INotification
{
  //Guid Id { get; }
}
