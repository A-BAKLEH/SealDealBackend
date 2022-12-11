using Clean.Architecture.Infrastructure.Data;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class AgencyQService
{
  private readonly AppDbContext _appDbContext;
  public AgencyQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  
}
