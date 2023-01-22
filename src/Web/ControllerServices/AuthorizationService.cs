
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.DTOs;
using Infrastructure.Data;
using SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Web.ApiModels.APIResponses.Broker;

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
  public async Task<SignedInBrokerDTO> VerifyAccountAsync(Guid id, string? TimeZoneId = null,bool createAgencyIfNotExists = false)
  {
    var response = new SignedInBrokerDTO();
    var broker = await _appDbContext.Brokers.Include(b => b.Agency).FirstOrDefaultAsync(b => b.Id == id);
    if (broker == null)
    {
      throw new InconsistentStateException("VerifyAccount", "Broker not found in DB", id.ToString());
    }
    response.AgencyId = broker.AgencyId;
    response.BrokerId = id;
    response.Created = broker.Created;
    response.FirstName = broker.FirstName;
    response.isAdmin = broker.isAdmin;
    response.LastName = broker.LastName;
    response.LoginEmail = broker.LoginEmail;
    response.PhoneNumber = broker.PhoneNumber;

    if(broker.TimeZoneId != TimeZoneId)
    {
      response.AccountStatus.TimeZoneChangeDetected= true;
      response.AccountStatus.MainTimeZone = broker.TimeZoneId;
      response.AccountStatus.DetectedTimeZone = TimeZoneId;
    }
      //TODO: maybe handle if account is active but subscription is not?
    if (broker.AccountActive)
    {
      response.AccountStatus.userAccountStatus = "active";
      response.AccountStatus.subscriptionStatus = broker.Agency.StripeSubscriptionStatus.ToString();
      response.AccountStatus.internalMessage = "ok";
      return response;
    }
    //account not active
    else if(broker.Agency.StripeSubscriptionStatus == StripeSubscriptionStatus.NoStripeSubscription && broker.isAdmin)
    {
      response.AccountStatus.userAccountStatus = "inactive";
      response.AccountStatus.subscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription.ToString();
      response.AccountStatus.internalMessage = "ok";
      return response;
    }
    else
    {
      //handle other possible cases if any
      return response;
    }
  }

}
