using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Web.MediatrRequests.LeadRequests;

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
          //.Where(l => l.Id == request.leadId && l.BrokerId == request.BrokerId)
          .Select(l => new AllahLeadDTO
          {
              LeadId = l.Id,
              brokerId = l.BrokerId,
              Budget = l.Budget,
              Emails = l.LeadEmails.Select(em => new LeadEmailDTO { email = em.EmailAddress, isMain = em.IsMain}),
              EntryDate = l.EntryDate.UtcDateTime,
              LeadFirstName = l.LeadFirstName,
              LeadLastName = l.LeadLastName,
              leadSource = l.source,
              leadSourceDetails = l.SourceDetails,
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
                  APName = ass.ActionPlan.Name
              }),
          }).FirstOrDefaultAsync(l => l.LeadId == request.leadId && l.brokerId == request.BrokerId);

        if (lead == null) return null;

        if (request.includeNotifs)
        {
            lead.LeadHistoryEvents = await _appDbContext.Notifications
            .OrderByDescending(n => n.EventTimeStamp)
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
                UnderlyingEventTimeStamp = n.EventTimeStamp
            }
            ).ToListAsync(cancellationToken);
        }

        return lead;

    }
}

