
using Clean.Architecture.Core.Config.Constants.LoggingConstants;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces;
using Clean.Architecture.Core.ExternalServiceInterfaces.StripeInterfaces;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;
public class AddBrokersRequest : IRequest<List<Broker>>, ITransactional
{
  public Broker admin { get; set; }
  public List<Broker> brokers { get; set; } = new();
}

public class AddBrokersRequestHandler : IRequestHandler<AddBrokersRequest, List<Broker>>
{
  private readonly IStripeSubscriptionService _stripeSubscriptionService;
  private readonly ILogger<AddBrokersRequestHandler> _logger;
  private readonly IMsGraphService _msGraphService;
  private readonly IRepository<Agency> _repository;
  public AddBrokersRequestHandler(IStripeSubscriptionService stripeService, IMsGraphService graphService, IRepository<Agency> repository, ILogger<AddBrokersRequestHandler> logger)
  {
    _stripeSubscriptionService = stripeService;
    _logger = logger;
    _msGraphService = graphService;
    _repository = repository;
  }

  public async Task<List<Broker>> Handle(AddBrokersRequest request, CancellationToken cancellationToken)
  {
    var agency = request.admin.Agency;
    int FreeBrokersCount = agency.NumberOfBrokersInSubscription - agency.NumberOfBrokersInDatabase;
    int NewBrokersCount = request.brokers.Count;
    int FinalQuantity = agency.NumberOfBrokersInSubscription;
    if (NewBrokersCount > FreeBrokersCount)
    {
      FinalQuantity = await _stripeSubscriptionService.AddSubscriptionQuantityAsync(agency.StripeSubscriptionId, NewBrokersCount - FreeBrokersCount, FinalQuantity);
      //TODO send email to admin to confirm subs change
      _logger.LogInformation("[{Tag}] Added {NewBrokersCount} brokers to Agency with AgencyId {AgencyId} and SubscriptionId {SubscriptionId}", TagConstants.AddBrokersRequest, NewBrokersCount, agency.Id, agency.StripeSubscriptionId);
    }
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
            failedBrokers.Add(broker);
            request.brokers.Remove(broker);
          }
        });

    await Task.WhenAll(tasks);
    //TODO send email to brokers to login and RENEW PASSWORD
    agency.AgencyBrokers.AddRange(request.brokers);
    agency.NumberOfBrokersInSubscription = FinalQuantity;
    agency.NumberOfBrokersInDatabase = agency.NumberOfBrokersInDatabase + request.brokers.Count;
    await _repository.UpdateAsync(agency);
    return failedBrokers;
  }
}

