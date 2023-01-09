
using Core.Config.Constants.LoggingConstants;
using Core.Domain.BrokerAggregate;
using Core.ExternalServiceInterfaces;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.Data;
using SharedKernel;
using MediatR;

namespace Web.MediatrRequests.BrokerRequests;
public class AddBrokersRequest : IRequest<List<Broker>>, ITransactional
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
    
    List<Broker> failedBrokers = new();
    var tasks = request.brokers
        .Select(async (broker) =>
        {
          try
          {
            string newId = await _msGraphService.createB2CUser(broker);
            broker.Id = Guid.Parse(newId);
            broker.AccountActive = true;
            broker.isAdmin = false;
            _logger.LogInformation("[{Tag}]Created B2C User with UserId {UserId} and LoginEmail {LoginEmail} ", TagConstants.AddBrokersRequest, newId, broker.LoginEmail);
          }
          catch (Exception ex)
          {
            _logger.LogCritical("[{Tag}] Error creating B2C User with LoginEmail {LoginEmail} with AgencyId {AgencyId}. Exception : {Exception}", TagConstants.AddBrokersRequest, broker.LoginEmail, agency.Id, ex.ToString());
            //failedBrokers.Add(broker);
            //request.brokers.Remove(broker);
            throw;
          }
        });

    await Task.WhenAll(tasks);
    if (NewBrokersCount > FreeBrokersCount)
    {
      FinalQuantity = await _stripeSubscriptionService.AddSubscriptionQuantityAsync(agency.StripeSubscriptionId, NewBrokersCount - FreeBrokersCount, FinalQuantity);
      //TODO send email to admin to confirm subs change
      _logger.LogInformation("[{Tag}] Added {NewBrokersCount} brokers to Agency with AgencyId {AgencyId} and SubscriptionId {SubscriptionId}", TagConstants.AddBrokersRequest, NewBrokersCount, agency.Id, agency.StripeSubscriptionId);
    }
    //TODO send email to brokers to login and RENEW PASSWORD
    agency.AgencyBrokers.AddRange(request.brokers);
    agency.NumberOfBrokersInSubscription = FinalQuantity;
    agency.NumberOfBrokersInDatabase = agency.NumberOfBrokersInDatabase + request.brokers.Count;
    await _appDbContext.SaveChangesAsync();
    return failedBrokers;
  }
}

