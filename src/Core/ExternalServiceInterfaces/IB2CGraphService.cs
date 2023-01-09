
using Core.Domain.BrokerAggregate;

namespace Core.ExternalServiceInterfaces;
public interface IB2CGraphService
{
  Task<string> createB2CUser(Broker broker);

  Task test();
}
