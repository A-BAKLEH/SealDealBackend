
using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.SharedKernel.Interfaces;
using MediatR;

namespace Clean.Architecture.Core.Commands_Handlers.Signup;
public class SignupCommand : IRequest
{
  public string AgencyName { get; set; }
  public string givenName { get; set; }
  public string surName { get; set; }
  public string email { get; set; }
  public Guid b2cId { get; set; }
}

public class SignupCommandHandler : IRequestHandler<SignupCommand>
{
  private readonly IRepository<Agency> _repository;

  public SignupCommandHandler(IRepository<Agency> repository)
  {
    _repository = repository;
  }

  public async Task<Unit> Handle(SignupCommand request, CancellationToken cancellationToken)
  {
    var broker = new Broker()
    {
      Id = request.b2cId,
      FirstName = request.givenName,
      LastName = request.surName,
      Email = request.email,
      isAdmin = true,
    };
    var agency = new Agency()
    {

      AgencyName = request.AgencyName,
      IsPaying = false,
      SoloBroker = true,
      AgencyBrokers = new List<Core.BrokerAggregate.Broker> { broker }
    };
    await _repository.AddAsync(agency);
    return Unit.Value;
  }
}

