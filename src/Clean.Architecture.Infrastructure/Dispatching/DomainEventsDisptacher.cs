using Autofac;
using Autofac.Core;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.DomainNotifications;
using MediatR;

namespace Clean.Architecture.Infrastructure.Dispatching;
public class DomainEventsDispatcher : IDomainEventsDispatcher
{
  private readonly IMediator _mediator;
  private readonly ILifetimeScope _scope;
  private readonly AppDbContext _ordersContext;

  public DomainEventsDispatcher(IMediator mediator, ILifetimeScope scope, AppDbContext ordersContext)
  {
    this._mediator = mediator;
    this._scope = scope;
    this._ordersContext = ordersContext;
  }

  /// <summary>
  /// disptaches domain events and adds Domain Event Notifications to dbcontext's internal list
  /// </summary>
  /// <returns></returns>
  public async Task DispatchDomainEventsAsync()
  {
    var domainEntities = this._ordersContext.ChangeTracker
        .Entries<EntityBase>()
        .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any()).ToList();
    if (!domainEntities.Any()) return;

    var domainEvents = domainEntities
        .SelectMany(x => x.Entity.DomainEvents)
        .ToList();

    //var domainEventNotifications = new List<IDomainEventNotification<IDomainEvent>>();
    foreach (var domainEvent in domainEvents)
    {
      Type domainEvenNotificationType = typeof(IDomainEventNotification<>);
      var domainNotificationWithGenericType = domainEvenNotificationType.MakeGenericType(domainEvent.GetType());
      var domainNotification = _scope.ResolveOptional(domainNotificationWithGenericType, new List<Parameter>
                {
                    new NamedParameter("domainEvent", domainEvent)
                });

      if (domainNotification != null)
      {
        _ordersContext.AddDomainEventNotification(domainNotification as IDomainEventNotification<IDomainEvent>);
      }
    }

    domainEntities
        .ForEach(entity => entity.Entity.ClearDomainEvents());

    var tasks = domainEvents
        .Select(async (domainEvent) =>
        {
          await _mediator.Publish(domainEvent);
        });

    await Task.WhenAll(tasks);

    /*foreach (var domainEventNotification in domainEventNotifications)
    {
        string type = domainEventNotification.GetType().FullName;
        var data = JsonConvert.SerializeObject(domainEventNotification);
        OutboxMessage outboxMessage = new OutboxMessage(
            domainEventNotification.DomainEvent.OccurredOn,
            type,
            data);
        this._ordersContext.OutboxMessages.Add(outboxMessage);
    }*/
  }
  public async Task EnqueueDomainEventNotificationsAsync()
  {
    if (_ordersContext.DomainEventNotifications == null) return;
    foreach (var domainEventNotification in _ordersContext.DomainEventNotifications)
    {
      //string type = domainEventNotification.GetType().FullName;
      //var l = domainEventNotification.DomainEvent;
      //var lType = l.GetType();
      //var data = JsonConvert.SerializeObject(domainEventNotification);
      //domainEventNotification.
      //Hangfire.BackgroundJob.Enqueue<IEmailSender>(x => x.SendOrderConfirmedEmailAsync(new Order { Name = data}));

      Console.WriteLine("enquing domainEventNotification");
      var id = Hangfire.BackgroundJob.Enqueue<IDomainNotificationProcessor>(x => x.ProcessDomainEventNotificationAsync(domainEventNotification));
      Console.WriteLine($"job id is {id} enqueued into hangfire by thread {Thread.CurrentThread.ManagedThreadId} at {DateTime.UtcNow}");
      //Hangfire.BackgroundJob.Schedule
    }
    _ordersContext.ClearDomainEventNotifications();
  }
}
