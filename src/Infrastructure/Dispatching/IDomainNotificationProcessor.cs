
using SharedKernel.DomainNotifications;
using MediatR;

namespace Infrastructure.Dispatching;
public interface IDomainNotificationProcessor
{
  Task ProcessDomainEventNotificationAsync(IDomainEventNotification<IDomainEvent> domainEvent);
}

public class DomainNotificationProcessor : IDomainNotificationProcessor
{
  private readonly IMediator _mediator;
  public DomainNotificationProcessor(IMediator mediator)
  {
    _mediator = mediator;
  }
  public async Task ProcessDomainEventNotificationAsync(IDomainEventNotification<IDomainEvent> domainNotification)
  {
    Console.WriteLine($" thread {Thread.CurrentThread.ManagedThreadId} publishing domainEventNotification" +
        $"with type {domainNotification.GetType().Name} at {DateTime.UtcNow}");
    await _mediator.Publish(domainNotification);
  }
}
