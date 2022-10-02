using Clean.Architecture.Infrastructure.Data;
using MediatR;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;
public class GetBrokerTodosRequest : IRequest
{
  public Guid BrokerId { get; set; }
}
public class GetBrokerTodosRequestHandler : IRequestHandler<GetBrokerTodosRequest>
{
  private readonly AppDbContext _appDbContext;
  public GetBrokerTodosRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  public Task<Unit> Handle(GetBrokerTodosRequest request, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

