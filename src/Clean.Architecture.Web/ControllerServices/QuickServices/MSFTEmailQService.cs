using Clean.Architecture.Core.Constants.ProblemDetailsTitles;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.ExternalServices;
using Clean.Architecture.SharedKernel.Exceptions;
using Humanizer;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class MSFTEmailQService
{
  private readonly AppDbContext _appDbContext;
  private readonly ADGraphWrapper _adGraphWrapper;
  public MSFTEmailQService(AppDbContext appDbContext, ADGraphWrapper aDGraph)
  {
    _appDbContext = appDbContext;
    _adGraphWrapper = aDGraph;
  }

  /// <summary>
  /// 
  /// </summary>
  public async Task ConnectEmail(Broker broker,string email, string TenantId)
  {
    if(!broker.Agency.HasAdminEmailConsent)
    {
      if(broker.isAdmin) throw new CustomBadRequestException($"Admin has not consented to email permissions yet", ProblemDetailsTitles.StartAdminConsentFlow);
      else throw new CustomBadRequestException($"Admin has not consented to email permissions yet and current user is not an admin", ProblemDetailsTitles.AgencyAdminMustConsent);
    }

    _adGraphWrapper.CreateClient(TenantId);
    bool connected = await _adGraphWrapper.testEmailConnected(email);
    if(!connected)
    {
      //TODO log critical error
      //throw new InconsistentStateException();
    }

    broker.FirstConnectedEmail = email;
    broker.ConnectedEmailStatus = ConnectedEmailStatus.Good;
    
    _appDbContext.SaveChanges();
  }

  /// <summary>
  /// will also add admin's email as his connected email
  /// </summary>
  /// <param name="broker"></param>
  /// <param name="email"></param>
  /// <param name="TenantId"></param>
  /// <returns></returns>
  public async Task HandleAdminConsented(Broker broker, string email, string TenantId)
  {
    broker.Agency.HasAdminEmailConsent = true;
    broker.Agency.AzureTenantID = TenantId;

    await this.ConnectEmail(broker, email, TenantId);
  }
}
