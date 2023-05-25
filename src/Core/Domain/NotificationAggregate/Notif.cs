using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.NotificationAggregate;

/// <summary>
/// respresents notification created by analyzer. 
/// Not displayed in lead notifs.  
/// </summary>
public class Notif : Entity<int>
{
    public Guid BrokerId { get; set; }
    public Broker Broker { get; set; }
    public int? LeadId { get; set; }
    public Lead? lead { get; set; }
    public DateTimeOffset CreatedTimeStamp { get; set; }
    public EventType NotifType { get; set; }
    /// <summary>
    /// when isSeen,dont show to user and its marked for deletion in nightly task.
    /// </summary>
    public bool isSeen { get; set; } = false;
    /// <summary>
    /// 1 to 4
    /// </summary>
    public byte priority { get; set; }
    /// <summary>
    /// AppEvent (int) Or email that we are reminding of. Depending on NotifType parse as string or int
    /// </summary>
    public string? EventId { get; set; }

}