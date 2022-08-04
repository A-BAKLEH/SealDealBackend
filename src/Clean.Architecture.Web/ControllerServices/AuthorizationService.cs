using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.BrokerAggregate.Specifications;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.ApiModels.Responses;

namespace Clean.Architecture.Web.AuthenticationAuthorization;

public class AuthorizationService
{
  private readonly IReadRepository<Broker> _brokerRepository;
  public AuthorizationService(IReadRepository<Broker> repo)
  {
    _brokerRepository = repo;
  }
  /// <summary>
  /// 
  /// </summary>
  /// <param name="id"></param>
  /// <returns>Tuple(broker, broker.accountActive, broker.isAdmin)</returns>
  /// <exception cref="Exception"></exception>
  public Tuple<Broker, bool, bool> AuthorizeUser(Guid id, Boolean includeAgency = false)
  {
    Broker broker;
    if(includeAgency) broker = _brokerRepository.GetBySpecAsync(new BrokerByIdWithAgencySpec(id)).Result;
    else broker = _brokerRepository.GetByIdAsync(id).Result;

    if (broker == null) throw new Exception("Broker not found in DB");
    return Tuple.Create(broker, broker.AccountActive, broker.isAdmin);
  }

  public SigninResponse signinSignupUser(Guid id)
  {
    var response = new SigninResponse();
    var broker = _brokerRepository.GetBySpecAsync(new BrokerByIdWithAgencySpec(id)).Result;
    if (broker == null) throw new Exception("Broker not found in DB");
    //TODO: maybe handle if account is active but subscription is not?
    if (broker.AccountActive)
    {
      response.UserAccountStatus = "active";
      response.SubscriptionStatus = broker.Agency.StripeSubscriptionStatus.ToString();
      return response;
    }
    //account not active
    else if(broker.Agency.StripeSubscriptionStatus == Core.AgencyAggregate.StripeSubscriptionStatus.NoStripeSubscription && broker.isAdmin)
    {
      response.SubscriptionStatus = "nostripesubscription";
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
