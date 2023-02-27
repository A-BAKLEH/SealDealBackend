using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Web.ApiModels.RequestDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Core.Domain.NotificationAggregate;
using Web.Constants;
using Web.Outbox;
using Web.Outbox.Config;
using SharedKernel.Exceptions;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.AgencyAggregate;

namespace Web.MediatrRequests.LeadRequests;

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
    if (!request.BrokerWhoRequested.isAdmin && dto.AssignToSelf == false)
      throw new CustomBadRequestException("lead has to be assigned to self for broker who is not admin", ProblemDetailsTitles.AssignToSelf);

    var timestamp = DateTimeOffset.UtcNow;
    var lead = new Lead
    {
      AgencyId = request.BrokerWhoRequested.AgencyId,
      BrokerId = brokerToAssignToId,
      Budget = dto.Budget,
      Email = dto.Email,
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
    lead.SourceDetails[NotificationJSONKeys.CreatedByFullName] = request.BrokerWhoRequested.FirstName + " " + request.BrokerWhoRequested.LastName;
    lead.LeadHistoryEvents = new();

    //Action plan handling?

    Notification notifCreation = new Notification
    {
      EventTimeStamp = timestamp,
      DeleteAfterProcessing = false,

      ProcessingStatus = ProcessingStatus.NoNeed,
      NotifyBroker = false,
      ReadByBroker = true,
      BrokerId = request.BrokerWhoRequested.Id,
      NotifType = NotifType.LeadCreated
    };
    Notification LeadAssignedNotif = null;
    if (brokerToAssignToId != null)
    {
      notifCreation.NotifType = NotifType.LeadCreated | NotifType.LeadAssigned;
      //if assiging to self
      if (!dto.AssignToSelf)
      {
        notifCreation.NotifProps[NotificationJSONKeys.AssignedToId] = brokerToAssignToId.ToString();
        notifCreation.NotifProps[NotificationJSONKeys.AssignedToFullName] = request.createLeadDTO.AssignToBrokerFullName;

        LeadAssignedNotif = new Notification
        {
          EventTimeStamp = timestamp,
          DeleteAfterProcessing = false,
          ProcessingStatus = ProcessingStatus.Scheduled,
          NotifyBroker = true,
          ReadByBroker = false,
          BrokerId = (Guid)brokerToAssignToId,
          NotifType = NotifType.LeadAssigned
        };
        LeadAssignedNotif.NotifProps[NotificationJSONKeys.AssignedById] = request.BrokerWhoRequested.Id.ToString();
        LeadAssignedNotif.NotifProps[NotificationJSONKeys.AssignedByFullName] = request.BrokerWhoRequested.FirstName + " " + request.BrokerWhoRequested.LastName;
        _appDbContext.Notifications.Add(LeadAssignedNotif);
      }
    }
    lead.LeadHistoryEvents.Add(notifCreation);
    if (LeadAssignedNotif != null) lead.LeadHistoryEvents.Add(LeadAssignedNotif);

    if (dto.TagsIds != null && dto.TagsIds.Any())
    {
      var tags = await _appDbContext.Tags.Where(t => t.BrokerId == request.BrokerWhoRequested.Id && dto.TagsIds.Contains(t.Id)).ToListAsync();
      lead.Tags = tags;
    }
    //TODO insecure to input text directly, check how to store, display notes
    lead.Note = new Note { NotesText = dto.leadNote ?? "" };
    if (LeadAssignedNotif != null)
    {
      LeadAssignedNotif.NotifProps[NotificationJSONKeys.AdminNote] = dto.leadNote;
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

    if (LeadAssignedNotif != null)
    {
      var notifId = LeadAssignedNotif.Id;
      var leadAssignedEvent = new LeadAssigned { NotifId = notifId };
      try
      {
        var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(leadAssignedEvent));
        OutboxMemCache.ScheduledDict.Add(notifId, HangfireJobId);
      }
      catch (Exception ex)
      {
        //TODO refactor log message
        _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for LeadAssigned Event for notif" +
          "with {NotifId} with error {Error}", notifId, ex.Message);
        OutboxMemCache.SchedulingErrorDict.Add(notifId, leadAssignedEvent);
      }
    }
    var response = new LeadForListDTO
    {
      Budget = lead.Budget,
      Email = lead.Email,
      EntryDate = lead.EntryDate.UtcDateTime,
      LeadFirstName = lead.LeadFirstName,
      LeadId = lead.Id,
      LeadLastName = lead.LeadLastName,
      source = lead.source.ToString(),
      LeadStatus = lead.LeadStatus.ToString(),
      leadType = lead.leadType.ToString(),
      PhoneNumber = lead.PhoneNumber,
      Note = lead.Note == null ? null : new NoteDTO { id = lead.Note.Id, NoteText = lead.Note.NotesText },
      Tags = lead.Tags == null ? null : lead.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
      leadSourceDetails = lead.SourceDetails
    };
    return response;
  }
}
