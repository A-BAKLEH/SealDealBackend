using Clean.Architecture.Core.Config;
using Clean.Architecture.Core.Domain.BrokerAggregate.Events;
using Newtonsoft.Json;

namespace Clean.Architecture.Core.DomainNotifications.BrokerNotifs.Test;
public class BrokerSignedUpNotif : DomainNotificationBase<BrokerSignedUpEvent>
{
  public Guid brokerId { get; set; }
  public string brokerName { get; set; }

  public BrokerSignedUpNotif(BrokerSignedUpEvent domainEvent) : base(domainEvent)
  {
    this.brokerId = domainEvent.brokerId;
    this.brokerName = domainEvent.brokerName;
    Console.WriteLine("BrokerSignedUpNotif just created\n");
  }
  [JsonConstructor]
  public BrokerSignedUpNotif(Guid brokerId, string brokerName) : base(null)
  {
    this.brokerId = brokerId;
    this.brokerName = brokerName;
    Console.WriteLine("BrokerSignedUpNotif just created in JSON\n");
  }
}
