using Clean.Architecture.Core.BrokerAggregate;

namespace Clean.Architecture.Core.Interfaces;
public interface IMsGraphService
{
  Task createB2CUsers(List<Broker> brokers);
}
