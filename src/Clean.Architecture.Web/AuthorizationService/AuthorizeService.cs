using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel.Interfaces;

namespace Clean.Architecture.Web.AuthenticationAuthorization;

public class AuthorizeService
{
  private readonly IReadRepository<Broker> _repository;

  public AuthorizeService(IReadRepository<Broker> repo)
  {
    _repository = repo;
  }
  public Tuple<Broker, int> AuthorizeUser(Guid id)
  {
    var broker = _repository.GetByIdAsync(id).Result;
    if (broker == null) throw new Exception("Broker not found in DB");
    return Tuple.Create(broker, broker.AgencyId);
  }
}
