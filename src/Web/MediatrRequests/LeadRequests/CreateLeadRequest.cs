﻿using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Web.ApiModels.RequestDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Web.MediatrRequests.LeadRequests;

public class CreateLeadRequest : IRequest<IEnumerable<LeadForListDTO>>
{

  public Broker BrokerWhoRequested { get; set; }
  public IEnumerable<CreateLeadDTO> createLeadDTOs { get; set; }
}

public class CreateLeadRequestHandler : IRequestHandler<CreateLeadRequest, IEnumerable<LeadForListDTO>>
{
  private readonly AppDbContext _appDbContext;
  public CreateLeadRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<IEnumerable<LeadForListDTO>> Handle(CreateLeadRequest request, CancellationToken cancellationToken)
  {
    List<Lead> added = new();
    Guid adminId = request.BrokerWhoRequested.Id;
    if(!request.BrokerWhoRequested.isAdmin)
    {
     // adminId = _appDbContext.b
    }
    foreach (var dto in request.createLeadDTOs)
    {
      bool sourceExists = Enum.TryParse<LeadSource>(dto.leadSource, true, out var leadSource);
      bool typeExists = Enum.TryParse<LeadType>(dto.leadType, true, out var leadType);

      var leadtype = typeExists ? leadType : LeadType.Unknown;
      var source = sourceExists ? leadSource : LeadSource.unknown;
      var lead = new Lead
      {
        AgencyId = request.BrokerWhoRequested.AgencyId,
        BrokerId = request.BrokerWhoRequested.Id,
        Budget = dto.Budget,
        Email = dto.Email,
        LeadFirstName = dto.LeadFirstName ?? "-",
        LeadLastName = dto.LeadLastName,
        PhoneNumber = dto.PhoneNumber,
        EntryDate = DateTimeOffset.UtcNow,
        Areas = dto.Areas,
        leadSourceDetails = dto.leadSourceDetails,
        leadType = leadtype,
        source = source
      };
      if (dto.TagsIds != null && dto.TagsIds.Any())
      {
        var tags = await _appDbContext.Tags.Where(t => t.BrokerId == request.BrokerWhoRequested.Id && dto.TagsIds.Contains(t.Id)).ToListAsync();
        lead.Tags = tags;
      }
      if (dto.ListingOfInterstId != null)
      {
        lead.ListingId = dto.ListingOfInterstId;
      }
      if (dto.leadNote != null)
      {
        //TODO insecure to input text directly, check how to store, display notes
        lead.Note = new Note { NotesText = dto.leadNote };
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
      added.Add(lead);
    }
    await _appDbContext.SaveChangesAsync();


    var response = added.Select(x => new LeadForListDTO
    {
      Budget = x.Budget,
      Email = x.Email,
      EntryDate = x.EntryDate.UtcDateTime,
      LeadFirstName = x.LeadFirstName,
      LeadId = x.Id,
      LeadLastName = x.LeadLastName,
      source = x.source.ToString(),
      leadSourceDetails = x.leadSourceDetails,
      LeadStatus = x.LeadStatus.ToString(),
      leadType = x.leadType.ToString(),
      PhoneNumber = x.PhoneNumber,
      Note = x.Note == null ? null : new NoteDTO { id = x.Note.Id, NoteText = x.Note.NotesText },
      Tags = x.Tags == null ? null : x.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName})
    });
    return response;
  }
}
