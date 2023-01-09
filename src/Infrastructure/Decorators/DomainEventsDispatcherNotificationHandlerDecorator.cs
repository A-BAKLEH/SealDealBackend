using Infrastructure.Dispatching;
using MediatR;

namespace Infrastructure.Decorators;
public class DomainEventsDispatcherNotificationHandlerDecorator<T> : INotificationHandler<T> where T : IDomainEvent
{
  private readonly INotificationHandler<T> _decorated;
  private readonly IDomainEventsDispatcher _domainEventsDispatcher;

  public DomainEventsDispatcherNotificationHandlerDecorator(
      IDomainEventsDispatcher domainEventsDispatcher,
      INotificationHandler<T> decorated)
  {
    _domainEventsDispatcher = domainEventsDispatcher;
    _decorated = decorated;
  }

  public async Task Handle(T notification, CancellationToken cancellationToken)
  {
    await this._decorated.Handle(notification, cancellationToken);
    Console.WriteLine("notification handler decorator finished handling, now dispatching domain events");
    await this._domainEventsDispatcher.DispatchDomainEventsAsync();
  }
}
