﻿
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.LeadAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using System;
using Web.ApiModels;

namespace Web.ControllerServices.QuickServices;

public class LeadQService
{
    private readonly AppDbContext _appDbContext;

    public LeadQService(AppDbContext appDbContext)
    {
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

        if(dto.Emails != null && dto.Emails.Any())
        {
            List<LeadEmail> toRemove = new();
            foreach(var email in lead.LeadEmails)
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
            foreach(var dtoEmail in dto.Emails)
            {
                if(!lead.LeadEmails.Any(e => e.EmailAddress == dtoEmail))
                {
                    lead.LeadEmails.Add( new LeadEmail { EmailAddress = dtoEmail});
                }
            }
            foreach(var email in lead.LeadEmails)
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

        if(dto.language != null && Enum.TryParse<Language>(dto.language, true, out var lang)) { lead.Language = lang; }
        await _appDbContext.SaveChangesAsync();

        var response = new LeadForListDTO
        {
            Budget = lead.Budget,
            Emails = lead.LeadEmails.Select(e => new LeadEmailDTO { email = e.EmailAddress, isMain = e.IsMain}).ToList(),
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

        return response;
    }

    public async Task DeleteLeadAsync(int leadId, Guid brokerId, bool isAdmin)
    {
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
          $"DELETE FROM [dbo].[Notifications] WHERE LeadId = {leadId};");
        var leadToDelete = new Lead { Id = leadId };
        _appDbContext.Remove(leadToDelete);
        await _appDbContext.SaveChangesAsync();
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
              Tags = l.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName })
          })
          .OrderByDescending(l => l.LeadId)
          .ToListAsync();

        return leads;
    }

    public async Task<List<LeadForListDTO>> GetUnAssignedLeadsAsync(Guid brokerId, int AgencyId)
    {
        var leads = await _appDbContext.Leads.Include(l =>l.LeadEmails).Where(l => l.AgencyId == AgencyId && l.BrokerId == null)
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

        return leads;
    }
    public async Task makeLeadEmailMain(int leadId, string Email)
    {
        await _appDbContext.Database.ExecuteSqlRawAsync($"UPDATE [dbo].[LeadEmails] SET IsMain = 1" +
            $" WHERE EmailAddress = '{Email}' AND LeadId = '{leadId}';");
    }
}
