using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.Dispatching;
using Clean.Architecture.SharedKernel;
using MediatR;

namespace Clean.Architecture.Infrastructure.Behaviors;
public class TransactionalBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactional, IRequest<TResponse>
{
  private AppDbContext _dbContext;
  private readonly IDomainEventsDispatcher _domainEventsDispatcher;

  public TransactionalBehavior(AppDbContext context, IDomainEventsDispatcher dispatcher)
  {
    _dbContext = context;
    _domainEventsDispatcher = dispatcher;
  }

  public async Task<TResponse> Handle
  (TRequest request, CancellationToken cancellationToken,
   RequestHandlerDelegate<TResponse> next)
  {
    try
    {
      Console.WriteLine("before \n");
      //using var transaction = _orderContext.Database.BeginTransaction();
      //return await _scopeHandler.RunInTransactionScope(()=> next());
      //Begin Transaction is not used cuz maybe nested transactinos created?? internet doesnt think so
      //using var scope =  new TransactionScope(TransactionScopeOption.Required, DefaultTransactionOptions, TransactionScopeAsyncFlowOption.Enabled);
      using var transaction = _dbContext.Database.BeginTransaction();
      var result = await next();

      //allows saving inside scope
      //dispatch domain events after handling, DomainEventNotifs are saved in DbContextList
      //domainEventDispatcher awaits all tasks, which might in turn create other domain events.
      //DomainEventHandlers dont create their own scopes, they just use the same Dispatcher which publishes
      //and awaits new Domain Events and adds Outbox events to DbContext'list
      await _domainEventsDispatcher.DispatchDomainEventsAsync();
      //Enqueue outbox commands
      await _domainEventsDispatcher.EnqueueDomainEventNotificationsAsync();
      transaction.Commit();

      //dispatch domain events just like done with Sample project is fine BUT MAKE SURE DomainEventNotifs
      //are saved in list and not touched

      //dispatch domain Event notifications make sure no replicas and all notifs from inner notifs are processed 
      return result;
    }
    finally
    {
      Console.WriteLine("after \n");
    }
  }
}
