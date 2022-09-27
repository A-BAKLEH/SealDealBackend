
using Clean.Architecture.Core.Domain.AgencyAggregate.Events;
using Clean.Architecture.Core.Domain.BrokerAggregate.Events;
using Clean.Architecture.SharedKernel;
using MediatR;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Core.Domain.BrokerAggregate;

namespace Clean.Architecture.Web.Handlers.AgencyHandlers.EventHandlers;
public class TestEventHandler : INotificationHandler<TestEvent>
{
  private readonly AppDbContext _appDbContext;
  public TestEventHandler(AppDbContext appDbContext, IExecutionContextAccessor accessor)
  {
    _appDbContext = appDbContext;
    Console.WriteLine($"test event handler id : {accessor.CorrelationId}");
  }

  public async Task Handle(TestEvent domainEvent, CancellationToken cancellationToken)
  {

    Console.WriteLine($"handling domain event TestEvent in Thread {Thread.CurrentThread.ManagedThreadId}");
    var broker = new Broker
    {
      Id = Guid.NewGuid(),
      AgencyId = domainEvent.AgencyId,
      AccountActive = false,
      FirstName = "abdul",
      isAdmin = true,
      LoginEmail = "lol@com",
      LastName = "abdul",
    };
    _appDbContext.Add(broker);
    await _appDbContext.SaveChangesAsync();
    broker.AddDomainEvent(new BrokerSignedUpEvent { brokerId = broker.Id });
  }
}
