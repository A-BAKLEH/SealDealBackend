using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.LeadRequests;

public class GetAllahLeadRequest : IRequest<AllahLeadDTO>
{
  public Guid BrokerId { get; set; }
  public int AgencyId { get; set; }
  public int leadId { get; set; }
  public bool includeNotifs { get; set; }
}
public class GetAllahLeadRequestHandler : IRequestHandler<GetAllahLeadRequest, AllahLeadDTO>
{
  private readonly AppDbContext _appDbContext;
  public GetAllahLeadRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  public async Task<AllahLeadDTO> Handle(GetAllahLeadRequest request, CancellationToken cancellationToken)
  {
    var lead = await _appDbContext.Leads
      .Where(l => l.Id == request.leadId && l.BrokerId == request.BrokerId)
      .Select(l => new AllahLeadDTO
      {
        //AreasOfInterest = l.AreasOfInterest.Select(a => new AreaDTO { id = a.Id, name = a.Name}),
        Budget = l.Budget,
        Email = l.Email,
        EntryDate = l.EntryDate,
        LeadFirstName = l.LeadFirstName,
        LeadLastName = l.LeadLastName,
        leadSource = l.source,
        leadSourceDetails = l.leadSourceDetails,
        LeadStatus = l.LeadStatus.ToString(),
        leadType = l.leadType.ToString(),
        Note = l.Note,
        PhoneNumber = l.PhoneNumber,
        Tags = l.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
        ActionPlanAssociations = l.ActionPlanAssociations.Select(ass => new ActionPlanAssociationDTO
        {
          Id = ass.Id,
          APID = (int)ass.ActionPlanId,
          APStatus = ass.ThisActionPlanStatus,
          APName = ass.ActionPlan.Title
        }),
        //ThisAgencyListingsOfInterest = l.ListingsOfInterest.Select(listing => new LeadListingDTO
        //{
        //  Address = listing.Listing.Address,
        //  Price = listing.Listing.Price,
        //  ClientComments = listing.ClientComments,
        //  ListingId = listing.ListingId
        //})
      }).FirstOrDefaultAsync(cancellationToken);
    if (lead == null) return null;

    if (request.includeNotifs)
    {
      lead.LeadHistoryEvents = await _appDbContext.Notifications
      .OrderByDescending(n => n.UnderlyingEventTimeStamp)
      .Where(n => n.LeadId == request.leadId)
      .Select(n => new NotifExpandedDTO
      {
        //BrokerComment = n.BrokerComment,
        id = n.Id,
        //NotifData = n.NotifData,
        NotifProps = n.NotifProps,
        NotifType = n.NotifType.ToString(),
        NotifyBroker = n.NotifyBroker,
        ReadByBroker = n.ReadByBroker,
        UnderlyingEventTimeStamp = n.UnderlyingEventTimeStamp
      }
      ).ToListAsync(cancellationToken);
    }

    return lead;

  }
}

