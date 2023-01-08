

using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.ExternalServices;
using Hangfire;
using Microsoft.Graph;

namespace Clean.Architecture.Web.Processing.EmailAutomation;

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

  /// <summary>
  /// for use with first sync where connectedEmail ID is unknown
  /// </summary>
  /// <param name="brokerId"></param>
  /// <param name="EmailNumber"></param>
  /// <param name="tenantId"></param>
  public void RenewSubscription(Guid brokerId,int EmailNumber, string tenantId)
  {
    var connEmail = _appDbContext.ConnectedEmails.FirstOrDefault(x => x.BrokerId==brokerId && x.EmailNumber == EmailNumber);
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ExpirationDateTime = SubsEnds
    };

    _aDGraphWrapper.CreateClient(tenantId);
    var UpdatedSubs = _aDGraphWrapper._graphClient
      .Subscriptions[connEmail.GraphSubscriptionId.ToString()]
      .Request()
      .UpdateAsync(subs)
      .Result;

    connEmail.SubsExpiryDate= SubsEnds;
    var nextRenewalDate = SubsEnds - TimeSpan.FromMinutes(60);
    string RenewalJobId = BackgroundJob.Schedule<SubscriptionService>(s => s.RenewSubscription(connEmail.Id, tenantId), nextRenewalDate);
    connEmail.SubsRenewalJobId= RenewalJobId;
  }

  /// <summary>
  /// for use when connEmailId is known
  /// </summary>
  /// <param name="connEmailId"></param>
  /// <param name="tenantId"></param>
  public void RenewSubscription(int connEmailId, string tenantId)
  {
    var connEmail = _appDbContext.ConnectedEmails.FirstOrDefault(x => x.Id == connEmailId);
    DateTimeOffset SubsEnds = DateTime.UtcNow + new TimeSpan(0, 4230, 0);

    var subs = new Subscription
    {
      ExpirationDateTime = SubsEnds
    };

    _aDGraphWrapper.CreateClient(tenantId);
    var UpdatedSubs = _aDGraphWrapper._graphClient
      .Subscriptions[connEmail.GraphSubscriptionId.ToString()]
      .Request()
      .UpdateAsync(subs)
      .Result;

    connEmail.SubsExpiryDate = SubsEnds;
    var nextRenewalDate = SubsEnds - TimeSpan.FromMinutes(60);
    string RenewalJobId = BackgroundJob.Schedule<SubscriptionService>(s => s.RenewSubscription(connEmail.Id, tenantId), nextRenewalDate);
  }
}
