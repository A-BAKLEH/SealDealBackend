
using Core.Domain.BrokerAggregate;

namespace Core.ExternalServiceInterfaces;
public interface IB2CGraphService
{
  /// <summary>
  /// returns UserId,password
  /// </summary>
  /// <param name="broker"></param>
  /// <returns></returns>
  Task<Tuple<string, string>> createB2CUser(Broker broker);

  Task test();
}
