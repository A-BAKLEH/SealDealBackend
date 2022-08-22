
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate.Specifications;
using Clean.Architecture.Core.DTOs;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.SharedKernel.Repositories;

namespace Clean.Architecture.Web.ControllerServices;

public class AuthorizationService
{
  private readonly IReadRepository<Broker> _brokerRepository;
  private readonly ILogger<AuthorizationService> _logger;
  public AuthorizationService(IReadRepository<Broker> repo, ILogger<AuthorizationService> logger)
  {
    _brokerRepository = repo;
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
    Broker broker;
    if(includeAgency) broker = await _brokerRepository.GetBySpecAsync(new BrokerByIdWithAgencySpec(id));
    else broker = await _brokerRepository.GetByIdAsync(id);

    if (broker == null) throw new InconsistentStateException("AuthorizeUser","Broker not found in DB",id.ToString());
    return Tuple.Create(broker, broker.AccountActive, broker.isAdmin);
  }

  public async Task<SigninResponseDTO> signinSignupUserAsync(Guid id)
  {
    var response = new SigninResponseDTO();
    var broker = await _brokerRepository.GetBySpecAsync(new BrokerByIdWithAgencySpec(id));
    if (broker == null) throw new InconsistentStateException("SigninSignup","Broker not found in DB",id.ToString());
    //TODO: maybe handle if account is active but subscription is not?
    if (broker.AccountActive)
    {
      response.UserAccountStatus = "active";
      response.SubscriptionStatus = broker.Agency.StripeSubscriptionStatus.ToString();
      return response;
    }
    //account not active
    else if(broker.Agency.StripeSubscriptionStatus == StripeSubscriptionStatus.NoStripeSubscription && broker.isAdmin)
    {
      response.SubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription.ToString();
      response.UserAccountStatus = "inactive";
      return response;
    }
    else
    {
      //handle other possible cases if any
      return response;
    }
  }

}
