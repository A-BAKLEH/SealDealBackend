namespace Core.Domain.NotificationAggregate.HelperObjects;

public abstract class AbstractEmailData
{
  public string EmailId { get; set; }

  public EmailProvider EmailProvider { get; set; }
  public string EmailText { get; set; } = "";
  public DateTime EmailTimeStamp { get; set; }
}
public enum EmailProvider { Gmail, Msft }
