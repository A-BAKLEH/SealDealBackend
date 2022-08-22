namespace Clean.Architecture.Infrastructure.Dispatching;
public interface IDomainEventsDispatcher
{
  Task DispatchDomainEventsAsync();

  Task EnqueueDomainEventNotificationsAsync();
}
