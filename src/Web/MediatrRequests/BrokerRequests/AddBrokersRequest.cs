using Core.Config.Constants.LoggingConstants;
using Core.Domain.BrokerAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using MediatR;
using Web.Constants;
using Web.Outbox;
using Web.Outbox.Config;

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
        request.admin.AppEvents = new();
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

                    var brokerCreationEvent = new AppEvent
                    {
                        EventTimeStamp = timestamp,
                        EventType = EventType.BrokerCreated,
                        NotifyBroker = true,
                        ProcessingStatus = ProcessingStatus.Scheduled, //to send password
                        ReadByBroker = true,
                    };
                    brokerCreationEvent.Props.Add(NotificationJSONKeys.TempPasswd, res.Item2);
                    broker.AppEvents = new() { brokerCreationEvent };
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

        var StripeEvent = new AppEvent
        {
            EventTimeStamp = timestamp,
            EventType = EventType.StripeSubsChanged,
            NotifyBroker = true,
            ProcessingStatus = ProcessingStatus.NoNeed,
            ReadByBroker = true,
        };
        request.admin.AppEvents.Add(StripeEvent);

        await Task.WhenAll(tasks);

        if (NewBrokersCount > FreeBrokersCount)
        {
            FinalQuantity = await _stripeSubscriptionService.AddSubscriptionQuantityAsync(agency.StripeSubscriptionId, NewBrokersCount - FreeBrokersCount, FinalQuantity);
            _logger.LogInformation("[{Tag}] Added {NewBrokersCount} brokers to Agency with AgencyId {AgencyId} and SubscriptionId {SubscriptionId}", TagConstants.AddBrokersRequest, NewBrokersCount, agency.Id, agency.StripeSubscriptionId);
        }

        agency.AgencyBrokers.AddRange(request.brokers);
        agency.NumberOfBrokersInSubscription = FinalQuantity;
        agency.NumberOfBrokersInDatabase = agency.NumberOfBrokersInDatabase + request.brokers.Count;
        request.admin.isSolo = false;
        await _appDbContext.SaveChangesAsync();

        //Send email to brokers to login and RENEW PASSWORD
        foreach (var b in request.brokers)
        {
            var appEventId = b.AppEvents[0].Id;
            var brokerCreated = new BrokerCreated { AppEventId = appEventId };
            try
            {
                var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(brokerCreated));
                _logger.LogInformation("{place} broker with Id {brokerId} has Email sending jobId {HangfireJobId}", "brokerCreation", b.Id, HangfireJobId);
            }
            catch (Exception ex)
            {
                //TODO refactor log message
                _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for BrokerCreated Event for appEvent" +
                  "with {appEventId} with error {Error}", appEventId, ex.Message);
                OutboxMemCache.SchedulingErrorDict.TryAdd(appEventId, brokerCreated);
            }
        }
        return failedBrokers;
    }
}

