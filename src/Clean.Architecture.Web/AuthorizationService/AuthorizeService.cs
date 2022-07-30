using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.BrokerAggregate.Specifications;
using Clean.Architecture.SharedKernel.Interfaces;
using Clean.Architecture.Web.ApiModels.Responses;

namespace Clean.Architecture.Web.AuthenticationAuthorization;

public class AuthorizeService
{
  private readonly IReadRepository<Broker> _brokerRepository;
  public AuthorizeService(IReadRepository<Broker> repo)
  {
    _brokerRepository = repo;
  }
  /// <summary>
  /// 
  /// </summary>
  /// <param name="id"></param>
  /// <returns>Tuple(broker, broker.accountActive, broker.isAdmin)</returns>
  /// <exception cref="Exception"></exception>
  public Tuple<Broker, bool, bool> AuthorizeUser(Guid id)
  {
    var broker = _brokerRepository.GetByIdAsync(id).Result;
    if (broker == null) throw new Exception("Broker not found in DB");
    return Tuple.Create(broker, broker.AccountActive, broker.isAdmin);
  }

  public SigninResponse signinSignupUser(Guid id)
  {
    var response = new SigninResponse();
    var broker = _brokerRepository.GetBySpecAsync(new BrokerByIdWithAgencySpec(id)).Result;
    if (broker == null) throw new Exception("Broker not found in DB");
    if (broker.AccountActive)
    {
      response.accountStatus = "active";
      return response;
    }
    //account not active
    else if(broker.Agency.AgencyStatus == Core.AgencyAggregate.AgencyStatus.JustSignedUp && broker.isAdmin)
    {
      response.accountStatus = "justsignedup";
      return response;
    }
    else
    {
      //handle other possible cases if any
      return response;
    }
  }

}
