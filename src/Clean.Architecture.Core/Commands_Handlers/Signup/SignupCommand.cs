﻿
using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.Commands_Handlers.DTOs;
using Clean.Architecture.SharedKernel.Interfaces;
using MediatR;

namespace Clean.Architecture.Core.Commands_Handlers.Signup;
public class SignupCommand : IRequest<SigninResponseDTO>
{
  public string AgencyName { get; set; }
  public string givenName { get; set; }
  public string surName { get; set; }
  public string email { get; set; }
  public Guid b2cId { get; set; }
}

public class SignupCommandHandler : IRequestHandler<SignupCommand, SigninResponseDTO>
{
  private readonly IRepository<Agency> _repository;
  private readonly IReadRepository<Broker> _brokerRepo;

  public SignupCommandHandler(IRepository<Agency> repository, IReadRepository<Broker> brokerRepo)
  {
    _repository = repository;
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
  public async Task<SigninResponseDTO> Handle(SignupCommand request, CancellationToken cancellationToken)
  {
    if (_brokerRepo.GetByIdAsync(request.b2cId).Result != null)
    {
      //log warning 
      throw new Exception("broker with B2C ID already exists in Brokers table");
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
      StripeSubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription,
      NumberOfBrokersInDatabase = 1,
      AgencyBrokers = new List<Core.BrokerAggregate.Broker> { broker }
    };
    await _repository.AddAsync(agency);
    return new SigninResponseDTO
    {
      UserAccountStatus = "inactive",
      SubscriptionStatus = "nostripesubscription"
    };
  }
}

