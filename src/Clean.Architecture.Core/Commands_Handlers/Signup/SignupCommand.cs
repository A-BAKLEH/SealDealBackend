
using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using MediatR;

namespace Clean.Architecture.Core.Commands_Handlers.Signup;
public class SignupCommand : IRequest<string>
{
  public string AgencyName { get; set; }
  public string givenName { get; set; }
  public string surName { get; set; }
  public string email { get; set; }
  public Guid b2cId { get; set; }
}

public class SignupCommandHandler : IRequestHandler<SignupCommand, string>
{
  private readonly IRepository<Agency> _repository;
  private readonly IReadRepository<Broker> _brokerRepo;

  public SignupCommandHandler(IRepository<Agency> repository)
  {
    _repository = repository;
  }

  /// <summary>
  /// flow only gets here if token is issued with "newUser" claim ,which means B2C account created but not 
  /// stored in our DB yet.
  /// if already stored in DB, log a warning and return account Status 
  /// </summary>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<string> Handle(SignupCommand request, CancellationToken cancellationToken)
  {
    if (_brokerRepo.GetByIdAsync(request.b2cId).Result != null)
    {
      //log warning 
      return "active";
    }
      var broker = new Broker()
    {
      Id = request.b2cId,
      FirstName = request.givenName,
      LastName = request.surName,
      Email = request.email,
      isAdmin = true,
      AccountActive = false
    };
    var agency = new Agency()
    {

      AgencyName = request.AgencyName,
      NumberOfBrokersInSubscription = 0,
      AgencyStatus = AgencyStatus.JustSignedUp,
      SoloBroker = true,
      AgencyBrokers = new List<Core.BrokerAggregate.Broker> { broker }
    };
    await _repository.AddAsync(agency);
    return "justsignedup";
  }
}

