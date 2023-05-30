using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels;
using Web.Constants;
using Web.Outbox.Config;
using Web.Outbox;

namespace Web.ControllerServices.QuickServices;

public class LeadQService
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<LeadQService> _logger;
    public LeadQService(AppDbContext appDbContext, ILogger<LeadQService> logger)
    {
        _logger = logger;
        _appDbContext = appDbContext;
    }

    public async Task<LeadForListDTO> UpdateLeadAsync(int LeadId, UpdateLeadDTO dto, Guid brokerID)
    {
        var lead = await _appDbContext.Leads
          .Include(l => l.Note)
          .Include(l => l.LeadEmails)
          .FirstAsync(l => l.Id == LeadId && l.BrokerId == brokerID);
        if (dto.LeadFirstName != null) lead.LeadFirstName = dto.LeadFirstName;
        if (dto.LeadLastName != null) lead.LeadLastName = dto.LeadLastName;
        if (dto.Areas != null) lead.Areas = dto.Areas;
        if (dto.LeadType != null)
        {
            if (Enum.TryParse<LeadType>(dto.LeadType, true, out var leadType)) lead.leadType = leadType;
            else throw new CustomBadRequestException($"input {dto.LeadType}", ProblemDetailsTitles.InvalidInput);
        }
        if (dto.Budget != null) lead.Budget = dto.Budget;

        if (dto.Emails != null && dto.Emails.Any())
        {
            List<LeadEmail> toRemove = new();
            foreach (var email in lead.LeadEmails)
            {
                //email stays
                if (dto.Emails.Any(e => e == email.EmailAddress)) continue;
                //email removed
                else { toRemove.Add(email); }
            }
            foreach (var remove in toRemove)
            {
                lead.LeadEmails.Remove(remove);
            }
            foreach (var dtoEmail in dto.Emails)
            {
                if (!lead.LeadEmails.Any(e => e.EmailAddress == dtoEmail))
                {
                    lead.LeadEmails.Add(new LeadEmail { EmailAddress = dtoEmail });
                }
            }
            foreach (var email in lead.LeadEmails)
            {
                if (email.EmailAddress == dto.Emails[0]) email.IsMain = true;
                else email.IsMain = false;
            }
        }

        if (dto.PhoneNumber != null) lead.PhoneNumber = dto.PhoneNumber;
        if (dto.LeadStatus != null)
        {
            if (Enum.TryParse<LeadStatus>(dto.LeadStatus, true, out var leadStatus)) lead.LeadStatus = leadStatus;
            else throw new CustomBadRequestException($"input {dto.LeadStatus}", ProblemDetailsTitles.InvalidInput);
        }
        if (dto.leadNote != null)
        {
            lead.Note.NotesText = dto.leadNote;
        }

        if (dto.language != null && Enum.TryParse<Language>(dto.language, true, out var lang)) { lead.Language = lang; }
        await _appDbContext.SaveChangesAsync();

        var response = new LeadForListDTO
        {
            Budget = lead.Budget,
            Emails = lead.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain }).ToList(),
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
            leadSourceDetails = lead.SourceDetails,
            language = lead.Language.ToString(),
        };
        var first = response.Emails.First(e => e.isMain);
        response.Emails.Remove(first);
        response.Emails.Insert(0, first);
        return response;
    }

    public async Task DeleteLeadAsync(int leadId, Guid brokerId, bool isAdmin)
    {
        using var trans = await _appDbContext.Database.BeginTransactionAsync();
        var lead = await _appDbContext.Leads.Include(l => l.ActionPlanAssociations.Where(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running))
          .ThenInclude(apa => apa.ActionTrackers.Where(a => a.ActionStatus == ActionStatus.ScheduledToStart || a.ActionStatus == ActionStatus.Failed))
          .Include(l => l.ToDoTasks.Where(t => t.IsDone == false))
          .AsNoTracking()
          .FirstAsync(l => l.Id == leadId);
        if (lead.BrokerId != null && lead.BrokerId != brokerId) throw new CustomBadRequestException("nop", ProblemDetailsTitles.UserNoPermission, 403);
        if (lead.BrokerId == null && !isAdmin) throw new CustomBadRequestException("nop", ProblemDetailsTitles.UserNoPermission, 403);

        if (lead.ActionPlanAssociations != null && lead.ActionPlanAssociations.Any())
        {
            foreach (var apass in lead.ActionPlanAssociations)
            {
                if (apass.ActionTrackers.Any())
                {
                    foreach (var ta in apass.ActionTrackers)
                    {
                        var jobId = ta.HangfireJobId;
                        if (jobId != null)
                            try
                            {
                                BackgroundJob.Delete(jobId);
                            }
                            catch (Exception) { }
                    }
                }
            }
        }
        if (lead.ToDoTasks != null && lead.ToDoTasks.Any())
        {
            foreach (var item in lead.ToDoTasks)
            {
                if (item.HangfireReminderId != null)
                    try
                    {
                        BackgroundJob.Delete(item.HangfireReminderId);
                    }
                    catch (Exception) { }
            }
        }
        //related todoTasks should be deleted by cascade
        //related Action plan associations should be deleted by cascade
        // Notifs will be deleted automatically. TODO see if u wanna move them to cold storage
        //the outbox handlers u dont have to do anything for now they are enqued on short term failure will be rare
        await _appDbContext.Database.ExecuteSqlRawAsync
          ($"DELETE FROM [dbo].[ToDoTasks] WHERE LeadId = {leadId};" +
          $"DELETE FROM [dbo].[Notifs] WHERE LeadId = {leadId};" +
          $"DELETE FROM [dbo].[AppEvents] WHERE LeadId = {leadId};" +
          $"DELETE FROM [dbo].[EmailEvents] WHERE LeadId = {leadId};");

        var leadToDelete = new Lead { Id = leadId };
        _appDbContext.Remove(leadToDelete);
        await _appDbContext.SaveChangesAsync();
        await trans.CommitAsync();
    }
    public async Task<List<LeadForListDTO>> GetLeadsAsync(Guid brokerId)
    {
        var leads = await _appDbContext.Leads.Include(l => l.LeadEmails).Where(l => l.BrokerId == brokerId)
          .Select(l => new LeadForListDTO
          {
              Budget = l.Budget,
              Emails = l.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain }).ToList(),
              EntryDate = l.EntryDate.UtcDateTime,
              LeadFirstName = l.LeadFirstName,
              LeadId = l.Id,
              LeadLastName = l.LeadLastName,
              leadSourceDetails = l.SourceDetails,
              LeadStatus = l.LeadStatus.ToString(),
              leadType = l.leadType.ToString(),
              PhoneNumber = l.PhoneNumber,
              source = l.source.ToString(),
              language = l.Language.ToString(),
              Tags = l.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
              RunningWorkflows = l.ActionPlanAssociations.Where(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running).Select(apa => new RunningLeadActionPlanDTO { ActionPlanId = (int) apa.ActionPlanId, ActionPlanName = apa.ActionPlan.Name })
          })
          .OrderByDescending(l => l.LeadId)
          .ToListAsync();
        foreach (var item in leads)
        {
            var first = item.Emails.First(e => e.isMain);
            item.Emails.Remove(first);
            item.Emails.Insert(0, first);
        }
        return leads;
    }

    public async Task<List<LeadForListDTO>> GetUnAssignedLeadsAsync(Guid brokerId, int AgencyId)
    {
        var leads = await _appDbContext.Leads.Include(l => l.LeadEmails).Where(l => l.AgencyId == AgencyId && l.BrokerId == null)
          .Select(l => new LeadForListDTO
          {
              Budget = l.Budget,
              Emails = l.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain }).ToList(),
              EntryDate = l.EntryDate.UtcDateTime,
              LeadFirstName = l.LeadFirstName,
              LeadId = l.Id,
              LeadLastName = l.LeadLastName,
              leadSourceDetails = l.SourceDetails,
              LeadStatus = l.LeadStatus.ToString(),
              leadType = l.leadType.ToString(),
              PhoneNumber = l.PhoneNumber,
              source = l.source.ToString(),
              Tags = l.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
              language = l.Language.ToString(),
          })
          .OrderByDescending(l => l.LeadId)
          .ToListAsync();

        foreach (var item in leads)
        {
            var first = item.Emails.First(e => e.isMain);
            item.Emails.Remove(first);
            item.Emails.Insert(0, first);
        }
        return leads;
    }


    public async Task AssignLeadToBroker(Guid adminId, Guid AssignToId, int LeadId)
    {
        //TODO later adjust for multiple admins
        var creationEvent = await _appDbContext.AppEvents
            .Include(e => e.lead)
            .FirstOrDefaultAsync(e => e.BrokerId == adminId && e.LeadId == LeadId && e.EventType == EventType.LeadCreated);

        creationEvent.lead.BrokerId = AssignToId;

        var brokerIds = new List<Guid> {adminId, AssignToId };
        var brokers = await _appDbContext.Brokers
            .Select(b => new { b.Id, b.isAdmin, b.FirstName, b.LastName})
            .Where(b => brokerIds.Contains(b.Id))
            .AsNoTracking()
            .ToListAsync();
        var adminBroker = brokers.FirstOrDefault(b => b.Id == adminId);
        var broker = brokers.FirstOrDefault(b => b.Id == AssignToId);

        //creationEvent.Props[NotificationJSONKeys.AssignedToId] = AssignToId.ToString();
        //creationEvent.Props[NotificationJSONKeys.AssignedToFullName] = $"{broker.FirstName} {broker.LastName}";
        //_appDbContext.Entry(creationEvent).Property(f => f.Props).IsModified = true;

        var AssignedToYouEvent = new AppEvent
        {
            BrokerId = AssignToId,
            LeadId = LeadId,
            EventType = EventType.LeadAssignedToYou,
            EventTimeStamp = DateTimeOffset.UtcNow,
            ProcessingStatus = ProcessingStatus.Scheduled,
            ReadByBroker = false,
        };
        AssignedToYouEvent.Props[NotificationJSONKeys.AssignedById] = adminId.ToString();
        AssignedToYouEvent.Props[NotificationJSONKeys.AssignedByFullName] = $"{adminBroker.FirstName} {adminBroker.LastName}";
        _appDbContext.AppEvents.Add(AssignedToYouEvent);

        var YouAssignedToBrokerEvent = new AppEvent
        {
            BrokerId = adminId,
            LeadId = LeadId,
            EventType = EventType.YouAssignedtoBroker,
            EventTimeStamp = DateTimeOffset.UtcNow,
            ProcessingStatus = ProcessingStatus.NoNeed,
            ReadByBroker = true,
        };
        YouAssignedToBrokerEvent.Props[NotificationJSONKeys.AssignedToId] = broker.Id.ToString();
        YouAssignedToBrokerEvent.Props[NotificationJSONKeys.AssignedToFullName] = $"{broker.FirstName} {broker.LastName}";
        _appDbContext.AppEvents.Add(YouAssignedToBrokerEvent);

        await _appDbContext.SaveChangesAsync();

        var notifId = AssignedToYouEvent.Id;
        var leadAssignedEvent = new LeadAssigned { AppEventId = notifId };
        try
        {
            var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(leadAssignedEvent));
        }
        catch (Exception ex)
        {
            //TODO refactor log message
            _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for LeadAssigned Event for notif" +
              "with {NotifId} with error {Error}", notifId, ex.Message);
            OutboxMemCache.SchedulingErrorDict.TryAdd(notifId, leadAssignedEvent);
        }
    }
}
