using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.LeadRequests;

public class CreateLeadRequest : IRequest
{
  public Guid BrokerId { get; set; }
  public int AgencyId { get; set; }
  public IEnumerable<CreateLeadDTO> createLeadDTOs { get; set; }
}

public class CreateLeadRequestHandler : IRequestHandler<CreateLeadRequest>
{
  private readonly AppDbContext _appDbContext;
  public CreateLeadRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<Unit> Handle(CreateLeadRequest request, CancellationToken cancellationToken)
  {
    foreach (var dto in request.createLeadDTOs)
    {

      var lead = new Lead
      {
        AgencyId = request.AgencyId,
        BrokerId = request.BrokerId,
        Budget = dto.Budget,
        Email = dto.Email,
        LeadFirstName = dto.LeadFirstName ?? "-",
        LeadLastName = dto.LeadLastName,
        PhoneNumber = dto.PhoneNumber,
        Areas = dto.Areas,
        leadSourceDetails = dto.leadSourceDetails,
        leadType = dto.leadType == null ? LeadType.Unknown : dto.leadType.ToLower().Contains("buy") ? LeadType.Buyer : LeadType.Renter,
        source = dto.leadSource == null ? LeadSource.unknown : dto.leadSource.ToLower().Contains("manual") ? LeadSource.manual : LeadSource.website
      };
      if (dto.TagsIds != null && dto.TagsIds.Any())
      {
        var tags = await _appDbContext.Tags.Where(t => t.BrokerId == request.BrokerId && dto.TagsIds.Contains(t.Id)).ToListAsync();
        lead.Tags = tags;
      }
      if (dto.ListingsOfInterstIds != null && dto.ListingsOfInterstIds.Any())
      {
        var listings = await _appDbContext.Listings.Where(l => l.BrokerId == request.BrokerId && dto.ListingsOfInterstIds.Contains(l.Id)).ToListAsync();
        foreach (var listing in listings)
        {
          _appDbContext.LeadListing.Add(new LeadListing { Lead = lead, Listing = listing });

        }
      }
      if (dto.leadNote != null)
      {
        //TODO insecure to input text directly, check how to store, display notes
        lead.Note = new Note { NotesText = dto.leadNote };
      }
      if (dto.TagToAdd != null)
      {
        if (!_appDbContext.Tags.Any(t => t.BrokerId == request.BrokerId && t.TagName == dto.TagToAdd))
        {
          var tag = new Tag { BrokerId = request.BrokerId, TagName = dto.TagToAdd };
          lead.Tags = new List<Tag> { tag };
        }
      }
      _appDbContext.Leads.Add(lead);
    }
    await _appDbContext.SaveChangesAsync();

    return Unit.Value;
  }
}
