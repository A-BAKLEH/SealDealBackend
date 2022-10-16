﻿using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class LeadQService
{
  private readonly AppDbContext _appDbContext;

  public LeadQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<List<LeadForListDTO>> GetLeadsAsync(Guid brokerId)
  {
    var leads = await _appDbContext.Leads.Where(l => l.BrokerId == brokerId)
      .Select(l => new LeadForListDTO
      { 
        Budget = l.Budget,
        Email = l.Email,
        EntryDate = l.EntryDate,
        LeadFirstName = l.LeadLastName,
        LeadId = l.Id,
        LeadLastName = l.LeadLastName,
        leadSourceDetails = l.leadSourceDetails,
        LeadStatus = l.LeadStatus.ToString(),
        leadType = l.leadType.ToString(),
        PhoneNumber = l.PhoneNumber,
        source = l.source.ToString()
      }).ToListAsync();

    return leads;
  }
}
