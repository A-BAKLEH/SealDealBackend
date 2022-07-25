using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel.Interfaces;

namespace Clean.Architecture.Web.AuthenticationAuthorization;

public class AuthorizeService
{
  private readonly IReadRepository<Broker> _brokerRepository;
  public AuthorizeService(IReadRepository<Broker> repo)
  {
    _brokerRepository = repo;
  }
  public Tuple<Broker, int, bool> AuthorizeUser(Guid id)
  {
    var broker = _brokerRepository.GetByIdAsync(id).Result;
    if (broker == null) throw new Exception("Broker not found in DB");
    return Tuple.Create(broker, broker.AgencyId, broker.isAdmin);
  }

}
