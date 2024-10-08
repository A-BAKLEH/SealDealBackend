﻿using Core.Config.Constants.LoggingConstants;
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
using Web.ApiModels.APIResponses;
using Web.Constants;
using Web.ControllerServices.StaticMethods;
using Web.Outbox;
using Web.Outbox.Config;

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
        if (dto.VerifyEmailAddress != null) lead.verifyEmailAddress = (bool)dto.VerifyEmailAddress;
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

        if (dto.PhoneNumber != null)
        {
            dto.PhoneNumber = string.Concat(dto.PhoneNumber.
                Where(c => !char.IsWhiteSpace(c) && c != '(' && c != ')' && c != '-' && c != '_'));
            lead.PhoneNumber = dto.PhoneNumber;
        }        
        if (dto.LeadStatus != null)
        {
            if (Enum.TryParse<LeadStatus>(dto.LeadStatus, true, out var leadStatus)) lead.LeadStatus = leadStatus;
            else throw new CustomBadRequestException($"input {dto.LeadStatus}", ProblemDetailsTitles.InvalidInput);
        }
        if (dto.leadNote != null)
        {
            if (lead.Note == null) lead.Note = new();
            lead.Note.NotesText = dto.leadNote;
        }

        if (dto.language != null && Enum.TryParse<Language>(dto.language, true, out var lang)) { lead.Language = lang; }
        await _appDbContext.SaveChangesAsync();

        var response = new LeadForListDTO
        {
            Budget = lead.Budget,
            verifyEmailAddress = lead.verifyEmailAddress,
            Emails = lead.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain }).ToList(),
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
        //related todoTasks should be deleted by cascade
        //related Action plan associations should be deleted by cascade
        // Notifs will be deleted automatically. TODO see if u wanna move them to cold storage
        //the outbox handlers u dont have to do anything for now they are enqued on short term failure will be rare
        await _appDbContext.Database.ExecuteSqlRawAsync
          ($"DELETE FROM \"ToDoTasks\" WHERE \"LeadId\" = {leadId};" +
          $"DELETE FROM \"Notifs\" WHERE \"LeadId\" = {leadId};" +
          $"DELETE FROM \"AppEvents\" WHERE \"LeadId\" = {leadId};" +
          $"DELETE FROM \"EmailEvents\" WHERE \"LeadId\" = {leadId};");

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
              verifyEmailAddress = l.verifyEmailAddress,
              Emails = l.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain }).ToList(),
              EntryDate = l.EntryDate,
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
              RunningWorkflows = l.ActionPlanAssociations.Where(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running).Select(apa => new RunningLeadActionPlanDTO { ActionPlanId = (int)apa.ActionPlanId, ActionPlanName = apa.ActionPlan.Name })
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
              verifyEmailAddress = l.verifyEmailAddress,
              Emails = l.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain }).ToList(),
              EntryDate = l.EntryDate,
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

    public async Task<DsahboardLeadStatusdto> GetDsahboardLeadStatsAsync(Guid brokerId)
    {
        var leads = await _appDbContext.Leads
            .Where(l => l.BrokerId == brokerId)
            .OrderByDescending(l => l.EntryDate)
            .Select(l => new { l.LeadStatus, l.EntryDate })
            .ToListAsync();
        var broker = await _appDbContext.Brokers
            .Where(b => b.Id == brokerId)
            .Select(b => new { b.TimeZoneId })
            .FirstOrDefaultAsync();

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(broker.TimeZoneId);
        var todaydate = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, timeZoneInfo).Date;
        var todayStart = todaydate.AddMinutes(0);

        var UTCstartDay = MyTimeZoneConverter.ConvertToUTC(timeZoneInfo, todayStart);
        var UTCStartYesterday = UTCstartDay.AddDays(-1);


        var newLeadsToday = leads
            .Where(l => l.EntryDate >= UTCstartDay).Count();
        var newLeadsYesterday = leads.Where(l => l.EntryDate >= UTCStartYesterday && l.EntryDate < UTCstartDay).Count();

        var hotLeads = 0;
        var activeLeads = 0;
        var slowLeads = 0;
        var coldLeads = 0;
        var closedLeads = 0;
        var DeadLeads = 0;

        leads.ForEach(l =>
        {
            if (l.LeadStatus == LeadStatus.Hot) hotLeads++;
            else if (l.LeadStatus == LeadStatus.Active) activeLeads++;
            else if (l.LeadStatus == LeadStatus.Slow) slowLeads++;
            else if (l.LeadStatus == LeadStatus.Cold) coldLeads++;
            else if (l.LeadStatus == LeadStatus.Closed) closedLeads++;
            else DeadLeads++;
        });
        var res = new DsahboardLeadStatusdto
        {
            NewLeadsToday = newLeadsToday,
            NewLeadsYesterday = newLeadsYesterday,
            HotLeads = hotLeads,
            activeLeads = activeLeads,
            slowLeads = slowLeads,
            coldLeads = coldLeads,
            closedLeads = closedLeads,
            deadLeads = DeadLeads
        };
        return res;
    }


    public async Task AssignLeadToBroker(Guid adminId, Guid AssignToId, int LeadId)
    {
        //TODO later adjust for multiple admins
        var creationEvent = await _appDbContext.AppEvents
            .Include(e => e.lead)
            .FirstOrDefaultAsync(e => e.BrokerId == adminId && e.LeadId == LeadId && e.EventType == EventType.LeadCreated);

        creationEvent.lead.BrokerId = AssignToId;

        var brokerIds = new List<Guid> { adminId, AssignToId };
        var brokers = await _appDbContext.Brokers
            .Select(b => new { b.Id, b.isAdmin, b.FirstName, b.LastName })
            .Where(b => brokerIds.Contains(b.Id))
            .AsNoTracking()
            .ToListAsync();
        var adminBroker = brokers.FirstOrDefault(b => b.Id == adminId);
        var broker = brokers.FirstOrDefault(b => b.Id == AssignToId);

        var AssignedToYouEvent = new AppEvent
        {
            BrokerId = AssignToId,
            LeadId = LeadId,
            EventType = EventType.LeadAssignedToYou,
            EventTimeStamp = DateTime.UtcNow,
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
            EventTimeStamp = DateTime.UtcNow,
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
            var HangfireJobId = BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(leadAssignedEvent, null, CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger.LogCritical("{tag} Hangfire error scheduling Outbox Disptacher for LeadAssigned Event for event" +
              "with {eventId} with error {error}", TagConstants.HangfireDispatch, notifId, ex.Message + " :" + ex.StackTrace);
            OutboxMemCache.SchedulingErrorDict.TryAdd(notifId, leadAssignedEvent);
        }
    }
}
