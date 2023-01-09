using Infrastructure.Data;

namespace Web.ControllerServices.QuickServices;

public class AgencyQService
{
  private readonly AppDbContext _appDbContext;
  public AgencyQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  
}
