namespace Clean.Architecture.Core.DTOs;
public class AccountStatusDTO
{
  public string SubscriptionStatus { get; set; }
  public string UserAccountStatus { get; set; }
  public string messageTodisplay { get; set; }
  public string internalMessage { get; set; } 
  public string routeUrl { get; set; }
}
