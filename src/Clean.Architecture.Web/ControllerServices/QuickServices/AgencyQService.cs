using System.Collections.Generic;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
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
  public async Task<List<AgencyListingDTO>> GetAgencyListings(int Agencyid, bool includeSold)
  {
    var query = _appDbContext.Listings
      .OrderByDescending(l => l.DateOfListing)
      .Where(l => l.AgencyId == Agencyid);

    if (!includeSold) query = query.Where(l => l.Status == ListingStatus.Listed);

    List<AgencyListingDTO> listings = await query
      .Select(l => new AgencyListingDTO
      {
        Address = l.Address,
        DateOfListing = l.DateOfListing,
        ListingURL = l.URL,
        Price = l.Price,
        Status = l.Status.ToString(),
        GeneratedLeadsCount = l.LeadsGenerated.Count,
        AssignedBrokers = l.BrokersAssigned.Select(b => new BrokerPerListingDTO
        {
          BrokerId = b.BrokerId,
          firstName = b.Broker.FirstName,
          lastName = b.Broker.LastName
        })
      })
      .ToListAsync();
    return listings;
  }
  public async Task<Listing> CreateListing(int AgencyId, CreateListingRequestDTO dto)
  {
    int brokersCount = 0;
    List<BrokerListingAssignment> brokers = new();
    if (dto.AssignedBrokersIds != null && dto.AssignedBrokersIds.Any())
    {
      brokersCount += dto.AssignedBrokersIds.Count;
      foreach(var b in dto.AssignedBrokersIds)
      {
        brokers.Add(new BrokerListingAssignment { assignmentDate = DateTime.UtcNow,BrokerId = b});
      }
    }

    var listing = new Listing
    { Address = dto.Address,
      DateOfListing = dto.DateOfListing,
      AgencyId = AgencyId,
      Price = dto.Price,
      URL = dto.URL,
      AssignedBrokersCount = brokersCount,
      BrokersAssigned = brokers
    };
    _appDbContext.Listings.Add(listing);
    await _appDbContext.SaveChangesAsync();
    return listing;
  }

  public async Task<string?> AssignListingToBroker(int listingId, Guid brokerId)
  {
    
    var listing = _appDbContext.Listings.Where(l => l.Id == listingId)
      .Include(l => l.BrokersAssigned)
      .FirstOrDefault();
    if (listing == null) return "listing Not Found";
    else if (listing.BrokersAssigned != null && listing.BrokersAssigned.Any(x => x.BrokerId == brokerId)) return "listing already assigned to Broker";
    else
    {
      BrokerListingAssignment brokerlisting = new() { assignmentDate = DateTime.UtcNow,BrokerId = brokerId}; 
      
      if(listing.BrokersAssigned != null) listing.BrokersAssigned.Add(brokerlisting);
      else listing.BrokersAssigned = new List<BrokerListingAssignment> { brokerlisting };

      listing.AssignedBrokersCount++;
      await _appDbContext.SaveChangesAsync();
      return null;
    }
    
  }

  public async Task<string?> DetachBrokerFromListing(int listingId, Guid brokerId)
  {

    var listing = _appDbContext.Listings.Where(l => l.Id == listingId)
      .Include(l => l.BrokersAssigned).FirstOrDefault();
    if (listing == null) return "listing Not Found";
    else if (listing.BrokersAssigned != null && !listing.BrokersAssigned.Any(l => l.BrokerId == brokerId)) return "listing already not related to broker";
    else
    {
      listing.BrokersAssigned.RemoveAll(b => b.BrokerId == brokerId);
      listing.AssignedBrokersCount--;
      await _appDbContext.SaveChangesAsync();
      return null;
    }
    
  }
}
