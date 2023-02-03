

using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Hangfire;
using Microsoft.Graph;

namespace Web.Processing.EmailAutomation;

/// <summary>
/// handles subscription resource in Graph API
/// </summary>
public class SubscriptionService
{
  private AppDbContext _appDbContext;
  private ADGraphWrapper _aDGraphWrapper;
  private ILogger<SubscriptionService> _logger;
  public SubscriptionService(AppDbContext appDbContext, ADGraphWrapper aDGraph, ILogger<SubscriptionService> logger)
  {
    _appDbContext= appDbContext;
    _aDGraphWrapper= aDGraph;
    _logger= logger;
  }

  public void HandleAdminConsentConflict(Guid brokerId, string email)
  {
    throw new NotImplementedException();
  }
  /// <summary>
  /// for use with first sync where connectedEmail ID is unknown
  /// </summary>
  /// <param name="brokerId"></param>
  /// <param name="EmailNumber"></param>
  /// <param name="tenantId"></param>
  public void RenewSubscription(string email)
  {
    var connEmail = _appDbContext.ConnectedEmails.FirstOrDefault(x => x.Email == email);
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ExpirationDateTime = SubsEnds
    };

    _aDGraphWrapper.CreateClient(connEmail.tenantId);
    var UpdatedSubs = _aDGraphWrapper._graphClient
      .Subscriptions[connEmail.GraphSubscriptionId.ToString()]
      .Request()
      .UpdateAsync(subs)
      .Result;

    connEmail.SubsExpiryDate= SubsEnds;
    var nextRenewalDate = SubsEnds - TimeSpan.FromMinutes(60);
    string RenewalJobId = BackgroundJob.Schedule<SubscriptionService>(s => s.RenewSubscription(connEmail.Email), nextRenewalDate);
    connEmail.SubsRenewalJobId= RenewalJobId;
  }
}
