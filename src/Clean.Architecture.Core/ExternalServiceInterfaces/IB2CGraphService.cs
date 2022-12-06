
using Clean.Architecture.Core.Domain.BrokerAggregate;

namespace Clean.Architecture.Core.ExternalServiceInterfaces;
public interface IB2CGraphService
{
  Task<string> createB2CUser(Broker broker);

  Task test();
}
