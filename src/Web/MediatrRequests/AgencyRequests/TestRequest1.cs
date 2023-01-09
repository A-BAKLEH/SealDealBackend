using Core.Domain.AgencyAggregate;
using Core.Domain.AgencyAggregate.Events;
using Infrastructure.Data;
using SharedKernel;
using MediatR;

namespace Web.MediatrRequests.AgencyRequests;
public class TestRequest1 : IRequest, ITransactional
{
  public string name { get; init; }

}

public class TestRequest1Handler : IRequestHandler<TestRequest1>
{
  public readonly AppDbContext _AppDbContext;
  private readonly IExecutionContextAccessor _accessor;
  public TestRequest1Handler(AppDbContext appDbContext, IExecutionContextAccessor accessor)
  {
    _AppDbContext = appDbContext;
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
    _AppDbContext.Add(agency);
    await _AppDbContext.SaveChangesAsync();
    Console.WriteLine($"handler Request id : {_accessor.CorrelationId}");
    agency.AddDomainEvent(new TestEvent { AgencyId = agency.Id});
    return Unit.Value;
  }
}

