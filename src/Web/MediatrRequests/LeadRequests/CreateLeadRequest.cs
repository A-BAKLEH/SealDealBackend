using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Exceptions;
using Web.ApiModels.RequestDTOs;
using Web.Constants;
using Web.Outbox;
using Web.Outbox.Config;

namespace Web.MediatrRequests.LeadRequests;

/// <summary>
/// manually create lead.
/// </summary>
public class CreateLeadRequest : IRequest<LeadForListDTO>
{
    public Broker BrokerWhoRequested { get; set; }
    public CreateLeadDTO createLeadDTO { get; set; }
}

public class CreateLeadRequestHandler : IRequestHandler<CreateLeadRequest, LeadForListDTO>
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<CreateLeadRequestHandler> _logger;
    public CreateLeadRequestHandler(AppDbContext appDbContext, ILogger<CreateLeadRequestHandler> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task<LeadForListDTO> Handle(CreateLeadRequest request, CancellationToken cancellationToken)
    {
        var dto = request.createLeadDTO;

        bool typeExists = Enum.TryParse<LeadType>(dto.leadType, true, out var leadType);
        var leadtype = typeExists ? leadType : LeadType.Unknown;
        var brokerToAssignToId = dto.AssignToSelf ? request.BrokerWhoRequested.Id : dto.AssignToBrokerId;
        if ((!request.BrokerWhoRequested.isAdmin || request.BrokerWhoRequested.isSolo) && dto.AssignToSelf == false)
            throw new CustomBadRequestException("lead has to be assigned to self for broker who is not admin", ProblemDetailsTitles.AssignToSelf);

        var timestamp = DateTime.UtcNow;
        var lead = new Lead
        {
            AgencyId = request.BrokerWhoRequested.AgencyId,
            BrokerId = brokerToAssignToId,
            Budget = dto.Budget,
            LeadFirstName = dto.LeadFirstName ?? "-",
            LeadLastName = dto.LeadLastName,
            PhoneNumber = dto.PhoneNumber,
            EntryDate = timestamp,
            Areas = dto.Areas,
            leadType = leadtype,
            source = LeadSource.manual,
            LeadStatus = LeadStatus.New,
            ListingId = dto.ListingOfInterstId,
        };
        if (request.createLeadDTO.language != null && Enum.TryParse<Language>(request.createLeadDTO.language, true, out var language))
        {
            lead.Language = language;
        }
        if (dto.Emails != null && dto.Emails.Any())
        {
            lead.LeadEmails = new List<LeadEmail>(dto.Emails.Count);
            foreach (var email in dto.Emails)
            {
                lead.LeadEmails.Add(new LeadEmail
                {
                    EmailAddress = email,
                    IsMain = false
                });
            }
            lead.LeadEmails[0].IsMain = true;
        }

        lead.SourceDetails[NotificationJSONKeys.CreatedByFullName] = request.BrokerWhoRequested.FirstName + " " + request.BrokerWhoRequested.LastName;
        lead.SourceDetails[NotificationJSONKeys.CreatedById] = request.BrokerWhoRequested.Id.ToString();
        lead.AppEvents = new();

        //Action plan handling?

        AppEvent leadCreationEvent = new AppEvent
        {
            EventTimeStamp = timestamp,
            DeleteAfterProcessing = false,

            ProcessingStatus = ProcessingStatus.NoNeed,
            ReadByBroker = true,
            BrokerId = request.BrokerWhoRequested.Id,
            EventType = EventType.LeadCreated
        };
        AppEvent LeadAssignedEvent = null;
        if (brokerToAssignToId != null)
        {
            if (dto.AssignToSelf) leadCreationEvent.ReadByBroker = true;
            leadCreationEvent.EventType = EventType.LeadCreated | EventType.YouAssignedtoBroker;
            //if assiging to self
            if (!dto.AssignToSelf)
            {
                leadCreationEvent.Props[NotificationJSONKeys.AssignedToId] = brokerToAssignToId.ToString();
                leadCreationEvent.Props[NotificationJSONKeys.AssignedToFullName] = request.createLeadDTO.AssignToBrokerFullName;

                LeadAssignedEvent = new AppEvent
                {
                    EventTimeStamp = timestamp,
                    DeleteAfterProcessing = false,
                    ProcessingStatus = ProcessingStatus.Scheduled,
                    ReadByBroker = false,
                    BrokerId = (Guid)brokerToAssignToId,
                    EventType = EventType.LeadAssignedToYou
                };
                LeadAssignedEvent.Props[NotificationJSONKeys.AssignedById] = request.BrokerWhoRequested.Id.ToString();
                LeadAssignedEvent.Props[NotificationJSONKeys.AssignedByFullName] = request.BrokerWhoRequested.FirstName + " " + request.BrokerWhoRequested.LastName;
                _appDbContext.AppEvents.Add(LeadAssignedEvent);
            }
        }
        lead.AppEvents.Add(leadCreationEvent);
        if (LeadAssignedEvent != null) lead.AppEvents.Add(LeadAssignedEvent);

        if (dto.TagsIds != null && dto.TagsIds.Any())
        {
            var tags = await _appDbContext.Tags.Where(t => t.BrokerId == request.BrokerWhoRequested.Id && dto.TagsIds.Contains(t.Id)).ToListAsync();
            lead.Tags = tags;
        }
        //TODO insecure to input text directly, check how to store, display notes
        lead.Note = new Note { NotesText = dto.leadNote ?? "" };
        if (LeadAssignedEvent != null)
        {
            LeadAssignedEvent.Props[NotificationJSONKeys.AdminNote] = dto.leadNote;
        }

        if (dto.TagToAdd != null)
        {
            if (!_appDbContext.Tags.Any(t => t.BrokerId == request.BrokerWhoRequested.Id && t.TagName == dto.TagToAdd))
            {
                var tag = new Tag { BrokerId = request.BrokerWhoRequested.Id, TagName = dto.TagToAdd };
                lead.Tags = new List<Tag> { tag };
            }
        }
        _appDbContext.Leads.Add(lead);

        if (request.createLeadDTO.ListingOfInterstId != null)
        {
            var listing = await _appDbContext.Listings.FirstAsync(l => l.Id == request.createLeadDTO.ListingOfInterstId);
            listing.LeadsGeneratedCount++;
        }
        //concnurrency handling for listing LeadsGeneratedCount

        //TECH
        bool saved = false;
        while (!saved)
        {
            try
            {
                await _appDbContext.SaveChangesAsync();
                saved = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is Listing)
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = entry.GetDatabaseValues();

                        databaseValues.TryGetValue("LeadsGeneratedCount", out int dbcount);
                        dbcount++;
                        Listing listing = (Listing)entry.Entity;
                        listing.LeadsGeneratedCount = dbcount;
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "Don't know how to handle concurrency conflicts for "
                            + entry.Metadata.Name);
                    }
                }
            }
        }

        if (LeadAssignedEvent != null && !dto.AssignToSelf)
        {
            var notifId = LeadAssignedEvent.Id;
            var leadAssignedEvent = new LeadAssigned { AppEventId = notifId };
            try
            {
                var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(leadAssignedEvent));
            }
            catch (Exception ex)
            {
                //TODO refactor log message
                _logger.LogCritical("{tag} Hangfire error scheduling Outbox Disptacher for LeadAssigned Event for event" +
                     "with {eventId} with error {error}", TagConstants.HangfireDispatch, notifId, ex.Message + " :" + ex.StackTrace);
                OutboxMemCache.SchedulingErrorDict.TryAdd(notifId, leadAssignedEvent);
            }
        }

        var response = new LeadForListDTO
        {
            Budget = lead.Budget,
            Emails = lead.LeadEmails.Select(em => new LeadEmailDTO { email = em.EmailAddress, isMain = em.IsMain }).ToList(),
            EntryDate = lead.EntryDate,
            LeadFirstName = lead.LeadFirstName,
            LeadId = lead.Id,
            LeadLastName = lead.LeadLastName,
            source = lead.source.ToString(),
            LeadStatus = lead.LeadStatus.ToString(),
            leadType = lead.leadType.ToString(),
            PhoneNumber = lead.PhoneNumber,
            Note = lead.Note == null ? null : new NoteDTO { id = lead.Note.Id, NoteText = lead.Note.NotesText },
            Tags = lead.Tags == null ? null : lead.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
            leadSourceDetails = lead.SourceDetails,
            language = lead.Language.ToString(),
        };
        return response;
    }
}
