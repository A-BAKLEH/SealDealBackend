namespace Infrastructure.Dispatching;
public interface IDomainEventsDispatcher
{
  Task DispatchDomainEventsAsync();

  void EnqueueDomainEventNotifications();
}
