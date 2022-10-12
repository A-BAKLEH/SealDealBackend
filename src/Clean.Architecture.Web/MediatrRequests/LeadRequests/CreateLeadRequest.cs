using Clean.Architecture.Core.Domain.LeadAggregate;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.RequestDTOs;
using MediatR;

namespace Clean.Architecture.Web.MediatrRequests.LeadRequests;

public class CreateLeadRequest : IRequest
{
  public Guid BrokerId { get; set; }
  public int AgencyId { get; set; }
  public IEnumerable<CreateLeadDTO> createLeadDTOs { get; set; }
}

public class CreateLeadRequestHandler : IRequestHandler<CreateLeadRequest>
{
  private AppDbContext _appDbContext;
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
      };
      if(dto.leadNote != null)
      {
        //TODO insecure to input text directly, check how to store, display notes
        lead.Notes = new List<Note> { new Note { NotesText = dto.leadNote } };
      }
      _appDbContext.Leads.Add(lead);
    }
    await _appDbContext.SaveChangesAsync();

    return Unit.Value;
  }
}
