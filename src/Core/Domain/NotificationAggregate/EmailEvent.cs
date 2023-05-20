using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.NotificationAggregate;

/// <summary>
/// Represents RECEIVED email.
/// ID is ID of the email in msft or google.
/// </summary>
public class EmailEvent : Entity<string>
{
    public Guid BrokerId { get; set; }
    public Broker Broker { get; set; }
    /// <summary>
    /// broker connected email address which recevied the email
    /// </summary>
    public string BrokerEmail { get; set; }
    public int? LeadId { get; set; }
    public Lead? lead { get; set; }
    public DateTimeOffset TimeReceived { get; set; }
    public bool Seen { get; set; } = false;
    /// <summary>
    /// can be null when NeedsAction is null, or when dont know
    /// </summary>
    public bool? RepliedTo { get; set; }
    /// <summary>
    /// needs reply or forward
    /// </summary>
    public bool? NeedsAction { get; set; } = null;
    /// <summary>
    /// true if lead was extracted from email
    /// </summary>
    public bool LeadParsedFromEmail { get; set; } = false;
    /// <summary>
    /// when not null, means email came from lead provider
    /// and this is the email it came from
    /// </summary>
    public string? LeadProviderEmail { get; set; }
}
