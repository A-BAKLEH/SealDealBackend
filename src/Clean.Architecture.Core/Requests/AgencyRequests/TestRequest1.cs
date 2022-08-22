using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.AgencyAggregate.Events;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;

namespace Clean.Architecture.Core.Requests.AgencyRequests;
public class TestRequest1 : IRequest, ITransactional
{
  public string name { get; init; }

}

public class TestRequest1Handler : IRequestHandler<TestRequest1>
{
  public readonly IRepository<Agency> _repo;
  private readonly IExecutionContextAccessor _accessor;
  public TestRequest1Handler(IRepository<Agency> repository, IExecutionContextAccessor accessor)
  {
    _repo = repository;
    _accessor = accessor;
  }
  public async Task<Unit> Handle(TestRequest1 request, CancellationToken cancellationToken)
  {
    var agency = new Agency
    {
      AgencyName = request.name,
      NumberOfBrokersInDatabase = 0,
      NumberOfBrokersInSubscription = 0,
      StripeSubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription
    };
    await _repo.AddAsync(agency);
    Console.WriteLine($"handler Request id : {_accessor.CorrelationId}");
    agency.AddDomainEvent(new TestEvent { AgencyId = agency.Id});
    return Unit.Value;
  }
}

