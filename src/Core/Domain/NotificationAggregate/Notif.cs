using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.NotificationAggregate;

/// <summary>
/// respresents notification created by analyzer
/// </summary>
public class Notif : Entity<int>
{
    public Guid BrokerId { get; set; }
    public Broker Broker { get; set; }
    public int? LeadId { get; set; }
    public Lead? lead { get; set; }
    public DateTimeOffset CreatedTimeStamp { get; set; }
    public EventType NotifType { get; set; }
    public bool isSeen { get; set; } = false;
    public byte priority { get; set; }
    /// <summary>
    /// AppEvent Or email that we are reminding of. Depending on NotifType parse as string or int
    /// </summary>
    public string? EventId { get; set; }

}