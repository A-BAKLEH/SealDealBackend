﻿
using Clean.Architecture.Core.Config;
using MediatR;

namespace Clean.Architecture.Infrastructure.Dispatching;
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
        $"with type {domainNotification.GetType().Name} and ID {domainNotification.Id} at {DateTime.UtcNow}");
    await _mediator.Publish(domainNotification);
  }
}
