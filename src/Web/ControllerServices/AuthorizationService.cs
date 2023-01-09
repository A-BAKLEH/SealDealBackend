
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.DTOs;
using Infrastructure.Data;
using SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Web.ControllerServices;

public class AuthorizationService
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<AuthorizationService> _logger;
  public AuthorizationService(AppDbContext appDbContext ,ILogger<AuthorizationService> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
  }
  /// <summary>
  /// 
  /// </summary>
  /// <param name="id"></param>
  /// <returns>Tuple(broker, broker.accountActive, broker.isAdmin)</returns>
  /// <exception cref="Exception"></exception>
  public async Task<Tuple<Broker, bool, bool>> AuthorizeUser(Guid id, Boolean includeAgency = false)
  {
    Broker? broker;
    if (includeAgency) broker = await _appDbContext.Brokers.Include(b =>b.Agency).FirstOrDefaultAsync(b => b.Id == id);
    else broker = await _appDbContext.Brokers.FirstOrDefaultAsync(b => b.Id == id);

    if (broker == null) throw new InconsistentStateException("AuthorizeUser","Broker not found in DB",id.ToString());
    return Tuple.Create(broker, broker.AccountActive, broker.isAdmin);
  }

  /// <summary>
  /// will create agency and broker if createAgencyIfNotExists == true and agency does not exist
  /// </summary>
  /// <param name="id"></param>
  /// <param name="createAgencyIfNotExists"></param>
  /// <returns></returns>
  /// <exception cref="InconsistentStateException"></exception>
  public async Task<AccountStatusDTO> VerifyAccountAsync(Guid id, string? TimeZoneId = null,bool createAgencyIfNotExists = false)
  {
    var response = new AccountStatusDTO();
    var broker = await _appDbContext.Brokers.Include(b => b.Agency).FirstOrDefaultAsync(b => b.Id == id);
    if (broker == null)
    {
      throw new InconsistentStateException("VerifyAccount", "Broker not found in DB", id.ToString());
    }

    if(broker.TimeZoneId != TimeZoneId)
    {
      response.TimeZoneChangeDetected= true;
      response.MainTimeZone = broker.TimeZoneId;
      response.DetectedTimeZone = TimeZoneId;
    }
      //TODO: maybe handle if account is active but subscription is not?
    if (broker.AccountActive)
    {
      response.userAccountStatus = "active";
      response.subscriptionStatus = broker.Agency.StripeSubscriptionStatus.ToString();
      response.internalMessage = "ok";
      return response;
    }
    //account not active
    else if(broker.Agency.StripeSubscriptionStatus == StripeSubscriptionStatus.NoStripeSubscription && broker.isAdmin)
    {
      response.userAccountStatus = "inactive";
      response.subscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription.ToString();
      response.internalMessage = "ok";
      return response;
    }
    else
    {
      //handle other possible cases if any
      return response;
    }
  }

}
