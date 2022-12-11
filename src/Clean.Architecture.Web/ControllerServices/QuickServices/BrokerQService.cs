using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.APIResponses.Broker;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class BrokerQService
{
  private readonly AppDbContext _appDbContext;
  public BrokerQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<List<BrokerForListDTO>> GetBrokersByAdmin(int AgencyId)
  {
    var brokers = await _appDbContext.Brokers
      .OrderByDescending(b => b.Created)
      .Where(b => b.AgencyId == AgencyId && b.isAdmin == false)
      .Select(b => new BrokerForListDTO
      {
        AccountActive = b.AccountActive,
        FirstName = b.FirstName,
        Id = b.Id,
        LastName = b.LastName,
        SigninEmail = b.LoginEmail,
        created = b.Created,
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
        Address = l.Listing.Address,
        DateOfListing = l.Listing.DateOfListing,
        ListingURL = l.Listing.URL,
        Price = l.Listing.Price,
        Status = l.Listing.Status.ToString(),
        DateAssignedToMe = l.assignmentDate,
        AssignedBrokersCount = l.Listing.BrokersAssigned.Count
      }).AsNoTracking().ToListAsync();
      
    return listings;
  }
}
