using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Core.ExternalServiceInterfaces;
using Core.Domain.NotificationAggregate;
using Web.Constants;
using SharedKernel.Exceptions;
using Core.Constants.ProblemDetailsTitles;
using Core.ExternalServiceInterfaces.StripeInterfaces;

namespace Web.ControllerServices.QuickServices;

public class BrokerQService
{
  private readonly AppDbContext _appDbContext;
  private readonly IB2CGraphService _b2CGraphService;
  private readonly IStripeSubscriptionService _stripeSubscriptionService;
  public BrokerQService(AppDbContext appDbContext,IB2CGraphService b2CGraphService, IStripeSubscriptionService stripeSubscriptionService)
  {
    _appDbContext = appDbContext;
    _b2CGraphService = b2CGraphService;
    _stripeSubscriptionService = stripeSubscriptionService;
  }

  public async Task SetTimeZoneAsync(Broker broker, string NewTimeZoneId)
  {
    broker.TimeZoneId = NewTimeZoneId;
    await _appDbContext.SaveChangesAsync();
  }
  public async Task<List<BrokerForListDTO>> GetBrokersByAdmin(int AgencyId)
  {
    var brokers = await _appDbContext.Brokers
      .OrderByDescending(b => b.Created)
      .Where(b => b.AgencyId == AgencyId)
      .Select(b => new BrokerForListDTO
      {
        AccountActive = b.AccountActive,
        FirstName = b.FirstName,
        Id = b.Id,
        LastName = b.LastName,
        SigninEmail = b.LoginEmail,
        created = b.Created.UtcDateTime,
        LeadsCount = b.Leads.Count,
        ListingsCount = b.AssignedListings.Count,
        PhoneNumber = b.PhoneNumber
      })
      .ToListAsync();
    return brokers;
  }

  public async Task<List<BrokerListingDTO>> GetBrokersListings(Guid brokerId)
  {

    var listings = await _appDbContext.BrokerListingAssignments
      .Where(b => b.BrokerId == brokerId)
      .OrderByDescending(a => a.assignmentDate)
      .Select(l => new BrokerListingDTO
      {
        ListingId = l.ListingId,
        Address = l.Listing.Address,
        DateOfListing = l.Listing.DateOfListing.UtcDateTime,
        ListingURL = l.Listing.URL,
        Price = l.Listing.Price,
        Status = l.Listing.Status.ToString(),
        DateAssignedToMe = l.assignmentDate,
        AssignedBrokersCount = l.Listing.BrokersAssigned.Count
      }).AsNoTracking().ToListAsync();
      
    return listings;
  }
  public async Task DeleteBrokerAsync(Guid brokerDeleteId, Guid userId, int AgencyId)
  {
    //check broker exists, belongs to this agency, is not an admin
    //var agency = await _appDbContext.Agencies
    //  .Include(a => a.AgencyBrokers.Where(b => b.Id == brokerDeleteId))
    //  .

    var broker = await _appDbContext.Brokers
      .Include(b => b.Agency)
      .FirstAsync(b => b.Id == brokerDeleteId && b.AgencyId == AgencyId);
    if (broker == null) throw new CustomBadRequestException("invalid",ProblemDetailsTitles.InvalidInput);

    //delete from B2C
    await _b2CGraphService.DeleteB2CUserAsync(brokerDeleteId);

    //Remove 1 quantity from Stripe
    var newQuantity = await _stripeSubscriptionService.DecreaseSubscriptionQuantityAsync(broker.Agency.StripeSubscriptionId, broker.Agency.NumberOfBrokersInSubscription);

    //decrease database number of brokers in DB and Stripe
    broker.Agency.NumberOfBrokersInSubscription = newQuantity;
    //broker.age

    //send notif to admin about stripe quantity change

    var StripeNotif = new Notification
    {
      EventTimeStamp = DateTimeOffset.UtcNow,
      NotifType = NotifType.StripeSubsChanged,
      NotifyBroker = false,
      ProcessingStatus = ProcessingStatus.Scheduled,
      ReadByBroker = true,
    };
    StripeNotif.NotifProps.Add(NotificationJSONKeys.EmailSent, "0");
  }
}
