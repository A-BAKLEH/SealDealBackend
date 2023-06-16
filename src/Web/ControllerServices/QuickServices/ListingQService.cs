using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels.RequestDTOs;
using Web.Constants;
using Web.ControllerServices.StaticMethods;
using Web.Outbox;
using Web.Outbox.Config;

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
    //public async Task<List<AgencyListingDTO>> GetAgencyListings(int Agencyid, bool includeSold)
    //{
    //  var query = _appDbContext.Listings
    //    .OrderByDescending(l => l.DateOfListing)
    //    .Where(l => l.AgencyId == Agencyid);

    //  if (!includeSold) query = query.Where(l => l.Status == ListingStatus.Listed);

    //  List<AgencyListingDTO> listings = await query
    //    .Select(l => new AgencyListingDTO
    //    {
    //      ListingId = l.Id,
    //      Address = new AddressDTO { StreetAddress = l.Address.StreetAddress, City = l.Address.City, Country = l.Address.Country, PostalCode = l.Address.PostalCode, ProvinceState = l.Address.ProvinceState },
    //      DateOfListing = l.DateOfListing.UtcDateTime,
    //      ListingURL = l.URL,
    //      Price = l.Price,
    //      Status = l.Status.ToString(),
    //      GeneratedLeadsCount = l.LeadsGeneratedCount,
    //      AssignedBrokers = l.BrokersAssigned.Select(b => new BrokerPerListingDTO
    //      {
    //        BrokerId = b.BrokerId,
    //        firstName = b.Broker.FirstName,
    //        lastName = b.Broker.LastName
    //      })
    //    }).AsNoTracking()
    //    .ToListAsync();
    //  return listings;
    //}

    public async Task<List<AgencyListingDTO>> GetListingsAsync(int Agencyid, Guid brokerId, bool isAdmin)
    {
        List<AgencyListingDTO> listings;

        if (isAdmin)
        {
            listings = await _appDbContext.Listings
            .OrderByDescending(l => l.DateOfListing)
            .Where(l => l.AgencyId == Agencyid)
            .Select(l => new AgencyListingDTO
            {
                ListingId = l.Id,
                Address = new AddressDTO { StreetAddress = l.Address.StreetAddress, apt = l.Address.apt, City = l.Address.City, Country = l.Address.Country, PostalCode = l.Address.PostalCode, ProvinceState = l.Address.ProvinceState },
                DateOfListing = l.DateOfListing,
                ListingURL = l.URL,
                Price = l.Price,
                Status = l.Status.ToString(),
                GeneratedLeadsCount = l.LeadsGeneratedCount,
                AssignedBrokers = l.BrokersAssigned.Select(b => new BrokerPerListingDTO
                {
                    BrokerId = b.BrokerId,
                    firstName = b.Broker.FirstName,
                    lastName = b.Broker.LastName
                }),
            }).AsNoTracking()
            .ToListAsync();
        }
        else
        {
            listings = await _appDbContext.BrokerListingAssignments
            .Where(bla => bla.BrokerId == brokerId)
            .Select(bla => new AgencyListingDTO
            {
                ListingId = bla.ListingId,
                Address = new AddressDTO { StreetAddress = bla.Listing.Address.StreetAddress, apt = bla.Listing.Address.apt, City = bla.Listing.Address.City, Country = bla.Listing.Address.Country, PostalCode = bla.Listing.Address.PostalCode, ProvinceState = bla.Listing.Address.ProvinceState },
                DateOfListing = bla.Listing.DateOfListing,
                ListingURL = bla.Listing.URL,
                Price = bla.Listing.Price,
                Status = bla.Listing.Status.ToString(),
                GeneratedLeadsCount = bla.Listing.LeadsGeneratedCount,
                AssignedBrokers = bla.Listing.BrokersAssigned.Select(b => new BrokerPerListingDTO
                {
                    BrokerId = b.BrokerId,
                    firstName = b.Broker.FirstName,
                    lastName = b.Broker.LastName
                })
            }).AsNoTracking()
            .ToListAsync();
        }
        return listings;
    }


    public async Task<AgencyListingDTO> CreateListing(int AgencyId, CreateListingRequestDTO dto, Guid UserId)
    {
        using var transaction = await _appDbContext.Database.BeginTransactionAsync();
        var timestamp = DateTime.UtcNow;

        byte brokersCount = 0;
        List<BrokerListingAssignment> brokersAssignments = new();

        if (dto.AssignedBrokersIds != null && dto.AssignedBrokersIds.Any())
        {
            brokersCount += (byte)dto.AssignedBrokersIds.Count;
            foreach (var b in dto.AssignedBrokersIds)
            {
                brokersAssignments.Add(new BrokerListingAssignment { assignmentDate = DateTime.UtcNow, BrokerId = b, isSeen = false });
            }
        }

        var streetAddress = dto.Address.StreetAddress.Replace("  ", " ").Trim();
        string apt = "";
        if (!string.IsNullOrWhiteSpace(dto.Address.apt))
        {
            apt = dto.Address.apt.Replace(" ", "");
        }
        var formatted = streetAddress.FormatStreetAddress();

        var listingStatus = ListingStatus.Listed;
        Enum.TryParse(dto.Status, true, out listingStatus);

        var listing = new Listing
        {
            Address = new Address
            {
                StreetAddress = streetAddress,
                apt = apt,
                City = dto.Address.City,
                Country = dto.Address.Country,
                PostalCode = dto.Address.PostalCode,
                ProvinceState = dto.Address.ProvinceState,
            },
            FormattedStreetAddress = formatted,
            DateOfListing = dto.DateOfListing,
            AgencyId = AgencyId,
            Price = dto.Price,
            URL = dto.URL,
            Status = listingStatus,
            AssignedBrokersCount = brokersCount,
            BrokersAssigned = brokersAssignments,
            LeadsGeneratedCount = 0
        };
        _appDbContext.Listings.Add(listing);

        var AppEvents = new List<AppEvent>();
        if (brokersAssignments.Any())
        {
            foreach (var ass in brokersAssignments)
            {
                var AppEvent = new AppEvent
                {
                    DeleteAfterProcessing = false,
                    BrokerId = ass.BrokerId,
                    EventTimeStamp = timestamp,
                    EventType = EventType.ListingAssigned,
                    ProcessingStatus = ProcessingStatus.Scheduled,
                    ReadByBroker = false
                };
                AppEvent.Props[NotificationJSONKeys.ListingId] = listing.Id.ToString();
                var add = listing.Address.StreetAddress; if (!string.IsNullOrWhiteSpace(apt)) add += "apt " + apt;
                add += ", " + listing.Address.City;
                AppEvent.Props[NotificationJSONKeys.ListingAddress] = add;
                AppEvent.Props[NotificationJSONKeys.UserId] = UserId.ToString();
                AppEvents.Add(AppEvent);
            }
            _appDbContext.AppEvents.AddRange(AppEvents);
            await _appDbContext.SaveChangesAsync();
            foreach (var appEvent in AppEvents)
            {
                var eventId = appEvent.Id;
                var ListingAssigned = new ListingAssigned { AppEventId = eventId };
                try
                {
                    var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(ListingAssigned));
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("{tag} Hangfire error scheduling Outbox Disptacher for ListingAssigned Event for event" +
                     "with {eventId} with error {error}", TagConstants.HangfireDispatch, eventId, ex.Message + " :" + ex.StackTrace);
                    OutboxMemCache.SchedulingErrorDict.TryAdd(eventId, ListingAssigned);
                }
            }
        }
        else await _appDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
        var listingDTO = new AgencyListingDTO
        {
            Address = new AddressDTO { StreetAddress = listing.Address.StreetAddress, apt = listing.Address.apt, City = listing.Address.City, Country = listing.Address.Country, PostalCode = listing.Address.PostalCode, ProvinceState = listing.Address.ProvinceState },
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

    public async Task EditListingAsync(int AgencyId, int listingId, EditListingDTO dto)
    {
        var listing = await _appDbContext.Listings.FirstAsync(l => l.Id == listingId && l.AgencyId == AgencyId);

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
    public async Task AssignListingToBroker(int AgencyId, int listingId, Guid brokerId, Guid userId)
    {

        var listing = _appDbContext.Listings.Where(l => l.Id == listingId && l.AgencyId == AgencyId)
          .Include(l => l.BrokersAssigned)
          .FirstOrDefault();
        if (listing == null) throw new CustomBadRequestException("not found", ProblemDetailsTitles.ListingNotFound, 404);
        else if (listing.BrokersAssigned != null && listing.BrokersAssigned.Any(x => x.BrokerId == brokerId))
        {
            throw new CustomBadRequestException("Already Assigned", ProblemDetailsTitles.ListingAlreadyAssigned);
        }

        else
        {
            BrokerListingAssignment brokerlisting = new() { assignmentDate = DateTime.UtcNow, BrokerId = brokerId, isSeen = false };

            if (listing.BrokersAssigned != null) listing.BrokersAssigned.Add(brokerlisting);
            else listing.BrokersAssigned = new List<BrokerListingAssignment> { brokerlisting };

            listing.AssignedBrokersCount++;
            var appEvent = new AppEvent
            {
                DeleteAfterProcessing = false,
                BrokerId = brokerId,
                EventTimeStamp = DateTime.UtcNow,
                EventType = EventType.ListingAssigned,
                ProcessingStatus = ProcessingStatus.Scheduled,
                ReadByBroker = false
            };
            appEvent.Props[NotificationJSONKeys.ListingId] = listingId.ToString();
            appEvent.Props[NotificationJSONKeys.UserId] = userId.ToString();
            var add = listing.Address.StreetAddress;
            var apt = listing.Address.apt;
            if(!string.IsNullOrWhiteSpace(apt)) add += "apt " + apt;
            add += ", " + listing.Address.City;
            appEvent.Props[NotificationJSONKeys.ListingAddress] = add;
            _appDbContext.AppEvents.Add(appEvent);
            await _appDbContext.SaveChangesAsync();

            var eventId = appEvent.Id;
            var ListingAssigned = new ListingAssigned { AppEventId = eventId };
            try
            {
                var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(ListingAssigned));
            }
            catch (Exception ex)
            {
                //TODO refactor log message
                _logger.LogCritical("{tag} Hangfire error scheduling Outbox Disptacher for LeadAssigned Event for event" +
                "with {eventId} with error {error}", TagConstants.HangfireDispatch, eventId, ex.Message + " :" + ex.StackTrace);
                OutboxMemCache.SchedulingErrorDict.TryAdd(eventId, ListingAssigned);
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

            var appEvent = new AppEvent
            {
                DeleteAfterProcessing = false,
                BrokerId = brokerId,
                EventTimeStamp = DateTime.UtcNow,
                EventType = EventType.ListingUnAssigned,
                ProcessingStatus = ProcessingStatus.NoNeed,
                ReadByBroker = false
            };
            appEvent.Props[NotificationJSONKeys.ListingId] = listingId.ToString();
            appEvent.Props[NotificationJSONKeys.UserId] = userId.ToString();
            _appDbContext.AppEvents.Add(appEvent);

            await _appDbContext.SaveChangesAsync();
        }
    }
    public async Task DeleteAgencyListingAsync(int listingId, int Agencyid, Guid userId)
    {
        var listing = await _appDbContext.Listings
          .Include(l => l.BrokersAssigned).FirstOrDefaultAsync(l => l.Id == listingId);
        if (listing == null) throw new CustomBadRequestException("not found", ProblemDetailsTitles.ListingNotFound, 404);

        foreach (var brokerAssignments in listing.BrokersAssigned)
        {
            var appEvent = new AppEvent
            {
                DeleteAfterProcessing = false,
                BrokerId = brokerAssignments.BrokerId,
                EventTimeStamp = DateTime.UtcNow,
                EventType = EventType.ListingUnAssigned,
                ProcessingStatus = ProcessingStatus.NoNeed,
                ReadByBroker = false
            };
            appEvent.Props[NotificationJSONKeys.UserId] = userId.ToString();
            _appDbContext.AppEvents.Add(appEvent);
        }

        var listingDelete = new Listing { Id = listingId, AgencyId = Agencyid };
        _appDbContext.Remove(listing);
        await _appDbContext.SaveChangesAsync();
    }
}
