using Clean.Architecture.Core.ExternalServiceInterfaces.ProcessingInterfaces;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;

namespace Clean.Architecture.Web.InterfaceImplementations;

public class RecTaskProcessor : IRecTaskProcessor
{
  private readonly AppDbContext _appDbContext;
  private readonly IDistributedCache _distributedCache;
  public RecTaskProcessor(AppDbContext appDbContext, IDistributedCache distributedCache)
  {
    _appDbContext = appDbContext;
    _distributedCache = distributedCache;
  }

  public async Task RunEmailsFetchTask(int TaskId, bool isGmail)
  {
    throw new NotImplementedException();
  }
}
