using Core.Constants.ProblemDetailsTitles;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using SharedKernel.Exceptions;
using Web.ApiModels.RequestDTOs;
using Microsoft.EntityFrameworkCore;
using Core.Domain.NotificationAggregate;
using Web.Constants;
using Web.Outbox.Config;
using Web.Outbox;

namespace Web.ControllerServices.QuickServices;

public class ListingQService
{
  private readonly AppDbContext _appDbContext;
  private readonly ILogger<ListingQService> _logger;
  public ListingQService(AppDbContext appDbContext, ILogger<ListingQService> logger)
  {
    _appDbContext = appDbContext;
    _logger = logger;
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
        ListingId = l.Id,
        Address = new AddressDTO { StreetAddress = l.Address.StreetAddress, City = l.Address.City, Country = l.Address.Country, PostalCode = l.Address.PostalCode, ProvinceState = l.Address.ProvinceState },
        DateOfListing = l.DateOfListing.UtcDateTime,
        ListingURL = l.URL,
        Price = l.Price,
        Status = l.Status.ToString(),
        GeneratedLeadsCount = l.LeadsGeneratedCount,
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
  public async Task<AgencyListingDTO> CreateListing(int AgencyId, CreateListingRequestDTO dto, Guid UserId)
  {
    using var transaction = _appDbContext.Database.BeginTransaction();
    var timestamp = DateTime.UtcNow;

    int brokersCount = 0;
    List<BrokerListingAssignment> brokersAssignments = new();

    if (dto.AssignedBrokersIds != null && dto.AssignedBrokersIds.Any())
    {
      brokersCount += dto.AssignedBrokersIds.Count;
      foreach (var b in dto.AssignedBrokersIds)
      {
        brokersAssignments.Add(new BrokerListingAssignment { assignmentDate = DateTime.UtcNow, BrokerId = b });
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
      BrokersAssigned = brokersAssignments,
      LeadsGeneratedCount = 0
    };
    _appDbContext.Listings.Add(listing);
    await _appDbContext.SaveChangesAsync();

    var notifs = new List<Notification>();
    if (brokersAssignments.Any())
    {
      foreach (var ass in brokersAssignments)
      {
        var notif = new Notification
        {
          DeleteAfterProcessing = false,
          BrokerId = ass.BrokerId,
          EventTimeStamp = timestamp,
          NotifType = NotifType.ListingAssigned,
          ProcessingStatus = ProcessingStatus.Scheduled,
          NotifyBroker = true,
          ReadByBroker = false
        };
        notif.NotifProps[NotificationJSONKeys.ListingId] = listing.Id.ToString();
        notif.NotifProps[NotificationJSONKeys.UserId] = UserId.ToString();
        notifs.Add(notif);
      }
      _appDbContext.Notifications.AddRange(notifs);
      await _appDbContext.SaveChangesAsync();
      foreach (var notif in notifs)
      {
        var notifId = notif.Id;
        var ListingAssigned = new ListingAssigned { NotifId = notifId };
        try
        {
          var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(ListingAssigned));
          OutboxMemCache.ScheduledDict.Add(notifId, HangfireJobId);
        }
        catch (Exception ex)
        {
          //TODO refactor log message
          _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for ListingAssigned Event for notif" +
            "with {NotifId} with error {Error}", notifId, ex.Message);
          OutboxMemCache.SchedulingErrorDict.Add(notifId, ListingAssigned);
        }
      }
    }

    transaction.Commit();
    var listingDTO = new AgencyListingDTO
    {
      Address = new AddressDTO { StreetAddress = listing.Address.StreetAddress, City = listing.Address.City, Country = listing.Address.Country, PostalCode = listing.Address.PostalCode, ProvinceState = listing.Address.ProvinceState },
      DateOfListing = listing.DateOfListing.UtcDateTime,
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

  public async Task EditListingAsync(int listingId, EditListingDTO dto)
  {
    var listing = new Listing
    {
      Id = listingId,
    };

    bool change = false;
    if (dto.Status != null && Enum.TryParse<ListingStatus>(dto.Status, true, out var lisStatus))
    {
      listing.Status = lisStatus;
      change = true;
    }
    if (dto.URL != null)
    {
      listing.URL = dto.URL;
      change = true;
    }
    if (dto.Price != null)
    { listing.Price = (int)dto.Price; change = true; }
    if (change)
    {
      _appDbContext.Listings.Update(listing);
      await _appDbContext.SaveChangesAsync();
    }

  }
  public async Task AssignListingToBroker(int listingId, Guid brokerId, Guid userId)
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
      var notif = new Notification
      {
        DeleteAfterProcessing = false,
        BrokerId = brokerId,
        EventTimeStamp = DateTime.UtcNow,
        NotifType = NotifType.ListingAssigned,
        ProcessingStatus = ProcessingStatus.Scheduled,
        NotifyBroker = true,
        ReadByBroker = false
      };
      notif.NotifProps[NotificationJSONKeys.ListingId] = listingId.ToString();
      notif.NotifProps[NotificationJSONKeys.UserId] = userId.ToString();
      _appDbContext.Notifications.Add(notif);
      await _appDbContext.SaveChangesAsync();

      var notifId = notif.Id;
      var ListingAssigned = new ListingAssigned { NotifId = notifId };
      try
      {
        var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(ListingAssigned));
        OutboxMemCache.ScheduledDict.Add(notifId, HangfireJobId);
      }
      catch (Exception ex)
      {
        //TODO refactor log message
        _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for ListingAssigned Event for notif" +
          "with {NotifId} with error {Error}", notifId, ex.Message);
        OutboxMemCache.SchedulingErrorDict.Add(notifId, ListingAssigned);
      }
    }
  }

  public async Task DetachBrokerFromListing(int listingId, Guid brokerId, Guid userId)
  {

    var listing = await _appDbContext.Listings
      .Include(l => l.BrokersAssigned).FirstOrDefaultAsync(l => l.Id == listingId);
    if (listing == null) throw new CustomBadRequestException("not found", ProblemDetailsTitles.ListingNotFound, 404);
    else if (listing.BrokersAssigned != null && !listing.BrokersAssigned.Any(l => l.BrokerId == brokerId)) return;
    else
    {
      listing.BrokersAssigned.RemoveAll(b => b.BrokerId == brokerId);
      listing.AssignedBrokersCount--;

      var notif = new Notification
      {
        DeleteAfterProcessing = false,
        BrokerId = brokerId,
        EventTimeStamp = DateTime.UtcNow,
        NotifType = NotifType.ListingUnAssigned,
        ProcessingStatus = ProcessingStatus.Scheduled,
        NotifyBroker = true,
        ReadByBroker = false
      };
      notif.NotifProps[NotificationJSONKeys.ListingId] = listingId.ToString();
      notif.NotifProps[NotificationJSONKeys.UserId] = userId.ToString();
      _appDbContext.Notifications.Add(notif);

      await _appDbContext.SaveChangesAsync();

      var notifId = notif.Id;
      var ListingUnAssigned = new ListingUnAssigned { NotifId = notifId };
      try
      {
        var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(ListingUnAssigned));
        OutboxMemCache.ScheduledDict.Add(notifId, HangfireJobId);
      }
      catch (Exception ex)
      {
        //TODO refactor log message
        _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for ListingUnAssigned Event for notif" +
          "with {NotifId} with error {Error}", notifId, ex.Message);
        OutboxMemCache.SchedulingErrorDict.Add(notifId, ListingUnAssigned);
      }
    }
  }
  public async Task DeleteAgencyListingAsync(int listingId, int Agencyid, Guid userId)
  {
    using var trans = _appDbContext.Database.BeginTransaction();

    var listing = await _appDbContext.Listings
      .Include(l => l.BrokersAssigned).FirstOrDefaultAsync(l => l.Id == listingId);
    if (listing == null) throw new CustomBadRequestException("not found", ProblemDetailsTitles.ListingNotFound, 404);

    List<Notification> notifs = new();
    foreach (var brokerAssignments in listing.BrokersAssigned)
    {
      var notif = new Notification
      {
        DeleteAfterProcessing = false,
        BrokerId = brokerAssignments.BrokerId,
        EventTimeStamp = DateTime.UtcNow,
        NotifType = NotifType.ListingUnAssigned,
        ProcessingStatus = ProcessingStatus.Scheduled,
        NotifyBroker = true,
        ReadByBroker = false
      };
      notif.NotifProps[NotificationJSONKeys.UserId] = userId.ToString();
      notifs.Add(notif);
    }
    _appDbContext.Notifications.AddRange(notifs);
    await _appDbContext.SaveChangesAsync();

    foreach (var notif in notifs)
    {
      var notifId = notif.Id;
      var ListingUnAssigned = new ListingUnAssigned { NotifId = notifId };
      try
      {
        var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(ListingUnAssigned));
        OutboxMemCache.ScheduledDict.Add(notifId, HangfireJobId);
      }
      catch (Exception ex)
      {
        //TODO refactor log message
        _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for ListingUnAssigned Event for notif" +
          "with {NotifId} with error {Error}", notifId, ex.Message);
        OutboxMemCache.SchedulingErrorDict.Add(notifId, ListingUnAssigned);
      }
    }
    var listingDelete = new Listing { Id = listingId, AgencyId = Agencyid };
    _appDbContext.Remove(listing);
    await _appDbContext.SaveChangesAsync();
    trans.Commit();
  }
}
