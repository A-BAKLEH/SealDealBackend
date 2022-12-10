using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ControllerServices;
using MediatR;

namespace Clean.Architecture.Web.MediatrRequests.SignupRequests;
public class SignupRequest : IRequest<AccountStatusDTO>
{
  public string AgencyName { get; set; }
  public string givenName { get; set; }
  public string surName { get; set; }
  public string email { get; set; }
  public Guid b2cId { get; set; }
}

public class SignupRequestHandler : IRequestHandler<SignupRequest, AccountStatusDTO>
{
  private readonly AppDbContext _appDbContext;
  private readonly AuthorizationService _authorizeService;
  private readonly ILogger<SignupRequestHandler> _logger;

  public SignupRequestHandler(AppDbContext appDbContext, AuthorizationService authorizeService, ILogger<SignupRequestHandler> logger)
  {
    _appDbContext = appDbContext;
    _authorizeService = authorizeService;
    _logger = logger;
  }

  /// <summary>
  /// flow only gets here if token is issued with "newUser" claim ,which means B2C account created but not 
  /// stored in our DB yet.
  /// if already stored in DB, log a warning and return account Status 
  /// </summary>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<AccountStatusDTO> Handle(SignupRequest request, CancellationToken cancellationToken)
  {
    if (_appDbContext.Brokers.FirstOrDefault(b => b.Id == request.b2cId) != null)
    {
      //log warning 
      //throw new InconsistentStateException("SignupRequest-UserAlreadyInDatabase","broker with B2C ID already exists in Brokers table", request.b2cId.ToString());
      _logger.LogWarning("broker with B2C ID already exists in Brokers table");
      var accountStatus = await this._authorizeService.VerifyAccountAsync(request.b2cId);

      return accountStatus;
    }
    var broker = new Broker()
    {
      Id = request.b2cId,
      FirstName = request.givenName,
      LastName = request.surName,
      LoginEmail = request.email,
      isAdmin = true,
      AccountActive = false
    };
    var agency = new Agency()
    {

      AgencyName = request.AgencyName,
      NumberOfBrokersInSubscription = 0,
      StripeSubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription,
      NumberOfBrokersInDatabase = 1,
      AgencyBrokers = new List<Broker> { broker }
    };
    _appDbContext.Add(agency);
    await _appDbContext.SaveChangesAsync();
    return new AccountStatusDTO
    {
      userAccountStatus = "inactive",
      subscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription.ToString(),
      internalMessage = "justSignedUp"
    };
  }
}

