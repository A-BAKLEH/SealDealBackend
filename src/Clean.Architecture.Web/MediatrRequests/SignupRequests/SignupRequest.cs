using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs;
using Clean.Architecture.SharedKernel.Exceptions;
using Clean.Architecture.SharedKernel.Repositories;
using MediatR;

namespace Clean.Architecture.Web.MediatrRequests.SignupRequests;
public class SignupRequest : IRequest<SigninResponseDTO>
{
  public string AgencyName { get; set; }
  public string givenName { get; set; }
  public string surName { get; set; }
  public string email { get; set; }
  public Guid b2cId { get; set; }
}

public class SignupRequestHandler : IRequestHandler<SignupRequest, SigninResponseDTO>
{
  private readonly IRepository<Agency> _agencyRepo;
  private readonly IReadRepository<Broker> _brokerRepo;

  public SignupRequestHandler(IRepository<Agency> agencyRepository, IReadRepository<Broker> brokerRepo)
  {
    _agencyRepo = agencyRepository;
    _brokerRepo = brokerRepo;
  }

  /// <summary>
  /// flow only gets here if token is issued with "newUser" claim ,which means B2C account created but not 
  /// stored in our DB yet.
  /// if already stored in DB, log a warning and return account Status 
  /// </summary>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<SigninResponseDTO> Handle(SignupRequest request, CancellationToken cancellationToken)
  {
    if (_brokerRepo.GetByIdAsync(request.b2cId).Result != null)
    {
      //log warning 
      throw new InconsistentStateException("SignupRequest-UserAlreadyInDatabase","broker with B2C ID already exists in Brokers table", request.b2cId.ToString());
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
    await _agencyRepo.AddAsync(agency);
    return new SigninResponseDTO
    {
      UserAccountStatus = "inactive",
      SubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription.ToString()
    };
  }
}

