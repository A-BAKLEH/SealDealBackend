using Autofac;
using Autofac.Core;
using Infrastructure.Data;
using SharedKernel;
using SharedKernel.DomainNotifications;
using MediatR;

namespace Infrastructure.Dispatching;
public class DomainEventsDispatcher : IDomainEventsDispatcher
{
  private readonly IMediator _mediator;
  private readonly ILifetimeScope _scope;
  private readonly AppDbContext _dbContext;

  public DomainEventsDispatcher(IMediator mediator, ILifetimeScope scope, AppDbContext ordersContext)
  {
    this._mediator = mediator;
    this._scope = scope;
    this._dbContext = ordersContext;
  }

  /// <summary>
  /// disptaches domain events and adds Domain Event Notifications from those same
  /// domain Events (if exists) to dbcontext's internal DomainNotifications list ot be enqueued into hangfire
  /// </summary>
  /// <returns></returns>
  public async Task DispatchDomainEventsAsync()
  {
    var domainEntities = this._dbContext.ChangeTracker
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
        _dbContext.AddDomainEventNotification(domainNotification as IDomainEventNotification<IDomainEvent>);
      }
    }

    domainEntities
        .ForEach(entity => entity.Entity.ClearDomainEvents());

    if (_dbContext.OnlyOutboxEvents) return;
    var tasks = domainEvents
        .Select(async (domainEvent) =>
        {
          await _mediator.Publish(domainEvent);
        });

    await Task.WhenAll(tasks);
  }
  /// <summary>
  /// Enqueues DomainNotifications saved in DbContext into Hangfire and then clears them
  /// </summary>
  public void EnqueueDomainEventNotifications()
  {
    if (_dbContext.DomainEventNotifications == null) return;
    foreach (var domainEventNotification in _dbContext.DomainEventNotifications)
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
    }
    _dbContext.ClearDomainEventNotifications();
  }
}
