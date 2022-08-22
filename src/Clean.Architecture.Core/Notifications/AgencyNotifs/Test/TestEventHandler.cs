
using Clean.Architecture.Core.Domain.AgencyAggregate.Events;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate.Events;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;

namespace Clean.Architecture.Core.Notifications.AgencyNotifs.Test;
public class TestEventHandler : INotificationHandler<TestEvent>
{
  private readonly IRepository<Broker> _repo;

  public TestEventHandler(IRepository<Broker> repo, IExecutionContextAccessor accessor)
  {
    _repo = repo;
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
      Email = "lol@com",
      LastName = "abdul"
    };
    await _repo.AddAsync(broker);
    broker.AddDomainEvent(new BrokerSignedUpEvent {brokerId = broker.Id });
  }
}
