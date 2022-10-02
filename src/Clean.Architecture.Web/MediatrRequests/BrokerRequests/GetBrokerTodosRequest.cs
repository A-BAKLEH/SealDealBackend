using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;
public class GetBrokerTodosRequest : IRequest<List<ToDoTask>>
{
  public Guid BrokerId { get; set; }
}
public class GetBrokerTodosRequestHandler : IRequestHandler<GetBrokerTodosRequest, List<ToDoTask>>
{
  private readonly AppDbContext _appDbContext;
  public GetBrokerTodosRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  public async Task<List<ToDoTask>> Handle(GetBrokerTodosRequest request, CancellationToken cancellationToken)
  {
    var todos = await _appDbContext.ToDoTasks.Where(todo => todo.BrokerId == request.BrokerId).ToListAsync();
    return todos;
  }
}

