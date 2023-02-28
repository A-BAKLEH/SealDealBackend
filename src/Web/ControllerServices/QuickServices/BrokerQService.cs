using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Web.ApiModels.APIResponses.Broker;
using Microsoft.EntityFrameworkCore;

namespace Web.ControllerServices.QuickServices;

public class BrokerQService
{
  private readonly AppDbContext _appDbContext;
  public BrokerQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
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
}
