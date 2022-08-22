
using Clean.Architecture.Core.Domain.BrokerAggregate;

namespace Clean.Architecture.Core.ServiceInterfaces;
public interface IMsGraphService
{
  Task createB2CUsers(List<Broker> brokers);
}
