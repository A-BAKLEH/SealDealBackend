
using Core.Config.Constants.LoggingConstants;
using Core.Domain.BrokerAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using MediatR;
using Web.Outbox.Config;
using Web.Outbox;
using Humanizer;

namespace Web.MediatrRequests.BrokerRequests;

/// <summary>
/// Removed Transactional
/// </summary>
public class AddBrokersRequest : IRequest<List<Broker>>
{
  public Broker admin { get; set; }
  public List<Broker> brokers { get; set; } = new();
}

public class AddBrokersRequestHandler : IRequestHandler<AddBrokersRequest, List<Broker>>
{
  private readonly IStripeSubscriptionService _stripeSubscriptionService;
  private readonly ILogger<AddBrokersRequestHandler> _logger;
  private readonly IB2CGraphService _msGraphService;
  private readonly AppDbContext _appDbContext;
  public AddBrokersRequestHandler(IStripeSubscriptionService stripeService, IB2CGraphService graphService, AppDbContext appDbContext, ILogger<AddBrokersRequestHandler> logger)
  {
    _stripeSubscriptionService = stripeService;
    _logger = logger;
    _msGraphService = graphService;
    _appDbContext = appDbContext;

  }

  public async Task<List<Broker>> Handle(AddBrokersRequest request, CancellationToken cancellationToken)
  {
    var agency = request.admin.Agency;
    int FreeBrokersCount = agency.NumberOfBrokersInSubscription - agency.NumberOfBrokersInDatabase;
    int NewBrokersCount = request.brokers.Count;
    int FinalQuantity = agency.NumberOfBrokersInSubscription;

    var timestamp = DateTime.UtcNow;

    List<Broker> failedBrokers = new();
    var tasks = request.brokers
        .Select(async (broker) =>
        {
          try
          {
            var res = await _msGraphService.createB2CUser(broker);
            var BrokerId = Guid.Parse(res.Item1);
            broker.Id = BrokerId;
            broker.AccountActive = true;
            broker.isAdmin = false;

            var notif = new Notification
            {
              EventTimeStamp = timestamp,
              NotifType = NotifType.BrokerCreated,
              NotifyBroker = false,
              ProcessingStatus = ProcessingStatus.Scheduled,
              ReadByBroker = true,
            };
            notif.NotifProps.Add("TempPassword", res.Item2);
            notif.NotifProps.Add("EmailSent", "0");
            broker.Notifs = new List<Notification> { notif };
            _logger.LogInformation("[{Tag}]Created B2C User with UserId {UserId} and LoginEmail {LoginEmail} ", TagConstants.AddBrokersRequest, res.Item1, broker.LoginEmail);
          }
          catch (Exception ex)
          {
            _logger.LogCritical("[{Tag}] Error creating B2C User with LoginEmail {LoginEmail} with AgencyId {AgencyId}. Exception : {Exception}", TagConstants.AddBrokersRequest, broker.LoginEmail, agency.Id, ex.ToString());
            //failedBrokers.Add(broker);
            //request.brokers.Remove(broker);
            throw;
          }
        });

    var StripeNotif = new Notification
    {
      EventTimeStamp = timestamp,
      NotifType = NotifType.StripeSubsChanged,
      NotifyBroker = false,
      ProcessingStatus = ProcessingStatus.Scheduled,
      ReadByBroker = true,
    };
    StripeNotif.NotifProps.Add("AdminEmailSent", "0");
    request.admin.Notifs = new List<Notification> { StripeNotif };

    await Task.WhenAll(tasks);

    if (NewBrokersCount > FreeBrokersCount)
    {
      FinalQuantity = await _stripeSubscriptionService.AddSubscriptionQuantityAsync(agency.StripeSubscriptionId, NewBrokersCount - FreeBrokersCount, FinalQuantity);
      //TODO send email to admin to confirm subs change
      //Hangfire.Enqueue.EmailSender
      _logger.LogInformation("[{Tag}] Added {NewBrokersCount} brokers to Agency with AgencyId {AgencyId} and SubscriptionId {SubscriptionId}", TagConstants.AddBrokersRequest, NewBrokersCount, agency.Id, agency.StripeSubscriptionId);
    }

    agency.AgencyBrokers.AddRange(request.brokers);
    agency.NumberOfBrokersInSubscription = FinalQuantity;
    agency.NumberOfBrokersInDatabase = agency.NumberOfBrokersInDatabase + request.brokers.Count;
    await _appDbContext.SaveChangesAsync();

    //Send email to brokers to login and RENEW PASSWORD
    foreach (var b in request.brokers)
    {
      var notifId = b.Notifs[0].Id;
      var brokerCreated = new BrokerCreated { NotifId = notifId };
      try
      {
        var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(brokerCreated));
        OutboxMemCache.ScheduledDictionary.Add(notifId,HangfireJobId);
      }
      catch(Exception ex)
      {
        //TODO refactor log message
        _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for BrokerCreated Event for notif" +
          "with {NotifId} with error {Error}",notifId,ex.Message);
        OutboxMemCache.ErrorDictionary.Add(notifId, brokerCreated);
      }
    }
    //Send email to admin to confirm Subscription Change
    var StripeNotifId = StripeNotif.Id;
    var stripeSubsChange = new StripeSubsChange { NotifId = StripeNotifId };
    try
    {
      var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(stripeSubsChange));
      OutboxMemCache.ScheduledDictionary.Add(StripeNotifId, HangfireJobId);
    }
    catch (Exception ex)
    {
      //TODO refactor log message
      _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for BrokerCreated Event for notif" +
        "with {NotifId} with error {Error}", StripeNotifId,ex.Message);
      OutboxMemCache.ErrorDictionary.Add(StripeNotifId, stripeSubsChange);
    }


    return failedBrokers;
  }
}

