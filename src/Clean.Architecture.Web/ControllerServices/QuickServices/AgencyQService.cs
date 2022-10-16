using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class AgencyQService
{
  private readonly AppDbContext _appDbContext;
  public AgencyQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  public async Task<List<ListingDTO>> GetAgencyListings(int Agencyid, bool includeSold)
  {
    var query = _appDbContext.Listings
      .OrderByDescending(l => l.DateOfListing)
      .Where(l => l.AgencyId == Agencyid);

    if (!includeSold) query = query.Where(l => l.Status == ListingStatus.Listed);

    List<ListingDTO> listings = await query
      .Select(l => new ListingDTO
      {
        Address = l.Address,
        DateOfListing = l.DateOfListing,
        ListingURL = l.URL,
        Price = l.Price,
        Status = l.Status.ToString(),
        InterestedLeadsCount = l.InterestedLeads.Count,
        AssignedBrokerId = l.BrokerId,
        BrokerName = l.AssignedBroker.FirstName,
        BrokerLName = l.AssignedBroker.LastName
      })
      .ToListAsync();
    return listings;
  }
  public async Task<Listing> CreateListing(int AgencyId, CreateListingRequestDTO dto)
  {
    var listing = new Listing
    { Address = dto.Address,
      DateOfListing = dto.DateOfListing,
      AgencyId =AgencyId,
      BrokerId = dto.BrokerId,
      Price = dto.Price,
      URL = dto.URL,
    };
    _appDbContext.Listings.Add(listing);
    await _appDbContext.SaveChangesAsync();
    return listing;
  }

  public async Task<string?> AssignListingToBroker(int listingId, Guid brokerId)
  {
    
    var listing = _appDbContext.Listings.Where(l => l.Id == listingId).FirstOrDefault();
    if (listing == null) return "listing Not Found";
    else if (listing.BrokerId != null) return "listing already assigned to Broker";
    else
    {
      listing.BrokerId = brokerId;
      await _appDbContext.SaveChangesAsync();
      return null;
    }
    
  }

  public async Task<string?> DetachBrokerFromListing(int listingId, Guid brokerId)
  {

    var listing = _appDbContext.Listings.Where(l => l.Id == listingId).FirstOrDefault();
    if (listing == null) return "listing Not Found";
    else if (listing.BrokerId != brokerId) return "listing already not related to broker";
    else
    {
      listing.BrokerId = null;
      await _appDbContext.SaveChangesAsync();
      return null;
    }
    
  }
}
