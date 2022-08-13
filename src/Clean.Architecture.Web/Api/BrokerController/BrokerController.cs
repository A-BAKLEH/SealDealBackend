

using Clean.Architecture.Core.DTOs;
using Clean.Architecture.Web.AuthenticationAuthorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.Web.Api.BrokerController;
public class BrokerController : BaseApiController
{
  public BrokerController(AuthorizationService authorizeService, IMediator mediator) : base(authorizeService, mediator)
  {
  }


  [Authorize]
  [HttpGet("get-subscription-quantities")]
  public async Task<IActionResult> GetCurrentSubscriptionQuantities()
  {
    var auth = User.Identity.IsAuthenticated;
    if (!auth) throw new Exception("not auth");

    var brokerTuple = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value), true);
    if (brokerTuple.Item2 == false || brokerTuple.Item3 == false) return Unauthorized();


    return Ok(new SubsQuantityDTO
    {
      StripeSubsQuantity = brokerTuple.Item1.Agency.NumberOfBrokersInSubscription,
      BrokersQuantity = brokerTuple.Item1.Agency.NumberOfBrokersInDatabase
    });
  }
  /*[Authorize]
  [HttpPost("add-brokers")]
  public async Task<IActionResult> AddBrokers([FromBody] IEnumerable<NewBrokerDTO> brokers)
  {
    var auth = User.Identity.IsAuthenticated;
    if (!auth) throw new Exception("not auth");

    var brokerTuple = this._authorizeService.AuthorizeUser(Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value));
    if (brokerTuple.Item2 == false || brokerTuple.Item3 == false) return Unauthorized();

    //var agencyObject = _agencyRepository.GetById(adminObj.AgencyId);
    var command = new AddBrokersCommand();
    List<NewBrokerDTO> nonValidBrokers = new();
    foreach (var broker in brokers)
    {
      if (BrokerDTOValid(broker)) command.brokers.Add(new Core.BrokerAggregate.Broker
      { 
        FirstName = broker.FirstName,
        LastName = broker.LastName,
        Email = broker.Email,
        PhoneNumber = broker.PhoneNumber
      });
      else nonValidBrokers.Add(broker);
    }
    await _mediator.Send(command);

    int numberOfNewBrokers = brokers.Count<NewBrokerDTO>();

    var services = new SubscriptionService();
    var stripeSubs = services.GetAsync(agencyObject.StripeSubscriptionId).Result;
    var quant = stripeSubs.Items.Data[0].Quantity;
    //susbItem.Data[0].Quantity = susbItem.Data[0].Quantity + numberOfNewBrokers;
    var items = new List<SubscriptionItemOptions>
           {
               new SubscriptionItemOptions{
                   Id = stripeSubs.Items.Data[0].Id,
                   Quantity = quant + numberOfNewBrokers,
               },
           };

    var options = new SubscriptionUpdateOptions
    {
      Items = items,
      CancelAtPeriodEnd = false,
    };

    stripeSubs = services.UpdateAsync(agencyObject.StripeSubscriptionId, options).Result;

    for (int i = 0; i < numberOfNewBrokers; i++)
    {
      var b2cBrokerId = Guid.Parse(_brokerService.addBroker(brokers.ElementAt<BrokerDTO>(i), adminObj.AgencyId));

      var newBroker = new Broker
      {
        BrokerId = b2cBrokerId,
        Email = brokers.ElementAt<BrokerDTO>(i).Email,
        FirstName = brokers.ElementAt<BrokerDTO>(i).FirstName,
        LastName = brokers.ElementAt<BrokerDTO>(i).LastName,
        PhoneNumber = brokers.ElementAt<BrokerDTO>(i).PhoneNumber,
        AgencyId = adminObj.AgencyId
      };

      _brokerRepository.Add(newBroker);

    }

  }

  public Boolean BrokerDTOValid(NewBrokerDTO brokerDTO)
  {
    return !string.IsNullOrWhiteSpace(brokerDTO.FirstName) && !string.IsNullOrWhiteSpace(brokerDTO.LastName) && !string.IsNullOrWhiteSpace(brokerDTO.Email);
  }*/


}
