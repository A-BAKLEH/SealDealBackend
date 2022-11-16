using System.Collections.Generic;
using Clean.Architecture.Core.Constants.ProblemDetailsTitles;
using Clean.Architecture.Core.Domain.AgencyAggregate;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.SharedKernel.Exceptions;
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
      }).AsNoTracking()
      .ToListAsync();
    return listings;
  }
  public async Task<AgencyListingDTO> CreateListing(int AgencyId, CreateListingRequestDTO dto)
  {
    int brokersCount = 0;
    List<BrokerListingAssignment> brokers = new();
    if (dto.AssignedBrokersIds != null && dto.AssignedBrokersIds.Any())
    {
      brokersCount += dto.AssignedBrokersIds.Count;
      foreach (var b in dto.AssignedBrokersIds)
      {
        brokers.Add(new BrokerListingAssignment { assignmentDate = DateTime.UtcNow, BrokerId = b });
      }
    }

    var listing = new Listing
    {
      Address = new Address
      {
        StreetAddress = dto.Address.StreetAddress,
        City = dto.Address.City,
        Country = dto.Address.Country,
        PostalCode = dto.Address.PostalCode,
        ProvinceState = dto.Address.ProvinceState,
      },
      DateOfListing = dto.DateOfListing,
      AgencyId = AgencyId,
      Price = dto.Price,
      URL = dto.URL,
      Status = dto.Status == "l" ? ListingStatus.Listed : ListingStatus.Sold,
      AssignedBrokersCount = brokersCount,
      BrokersAssigned = brokers
    };
    _appDbContext.Listings.Add(listing);
    await _appDbContext.SaveChangesAsync();
    var listingDTO = new AgencyListingDTO
    {
      Address = listing.Address,
      DateOfListing = listing.DateOfListing,
      GeneratedLeadsCount = 0,
      ListingURL = listing.URL,
      Price = listing.Price,
      Status = listing.Status.ToString(),
      AssignedBrokers = listing.BrokersAssigned.Select(b => new BrokerPerListingDTO
      {
        BrokerId = b.BrokerId,
      })
    };
    return listingDTO;
  }

  public async Task AssignListingToBroker(int listingId, Guid brokerId)
  {

    var listing = _appDbContext.Listings.Where(l => l.Id == listingId)
      .Include(l => l.BrokersAssigned)
      .FirstOrDefault();
    if (listing == null) throw new CustomBadRequestException("not found", ProblemDetailsTitles.ListingNotFound, 404);
    else if (listing.BrokersAssigned != null && listing.BrokersAssigned.Any(x => x.BrokerId == brokerId))
    {
      throw new CustomBadRequestException("Already Assigned", ProblemDetailsTitles.ListingAlreadyAssigned);
    }

    else
    {
      BrokerListingAssignment brokerlisting = new() { assignmentDate = DateTime.UtcNow, BrokerId = brokerId };

      if (listing.BrokersAssigned != null) listing.BrokersAssigned.Add(brokerlisting);
      else listing.BrokersAssigned = new List<BrokerListingAssignment> { brokerlisting };

      listing.AssignedBrokersCount++;
      await _appDbContext.SaveChangesAsync();
    }
  }

  public async Task DetachBrokerFromListing(int listingId, Guid brokerId)
  {

    var listing = _appDbContext.Listings.Where(l => l.Id == listingId)
      .Include(l => l.BrokersAssigned).FirstOrDefault();
    if (listing == null) throw new CustomBadRequestException("not found", ProblemDetailsTitles.ListingNotFound, 404);
    else if (listing.BrokersAssigned != null && !listing.BrokersAssigned.Any(l => l.BrokerId == brokerId)) return;
    else
    {
      listing.BrokersAssigned.RemoveAll(b => b.BrokerId == brokerId);
      listing.AssignedBrokersCount--;
      await _appDbContext.SaveChangesAsync();
    }

  }
}
