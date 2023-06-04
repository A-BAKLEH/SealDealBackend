using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Config.EnumExtens;

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
        var lead = new AllahLeadDTO();
        if (!request.includeNotifs)
        {
            lead = await _appDbContext.Leads
          .Select(l => new AllahLeadDTO
          {
              LeadId = l.Id,
              verifyEmailAddress = l.verifyEmailAddress,
              brokerId = l.BrokerId,
              Budget = l.Budget,
              Emails = l.LeadEmails.Select(em => new LeadEmailDTO { email = em.EmailAddress, isMain = em.IsMain }).ToList(),
              EntryDate = l.EntryDate.UtcDateTime,
              LeadFirstName = l.LeadFirstName,
              LeadLastName = l.LeadLastName,
              language = l.Language.ToString(),
              LastNotifsViewedAt = l.LastNotifsViewedAt,
              leadSource = l.source,
              leadSourceDetails = l.SourceDetails,
              LeadStatus = l.LeadStatus.ToString(),
              leadType = l.leadType.ToString(),
              Note = l.Note,
              PhoneNumber = l.PhoneNumber,
              Tags = l.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
              ActionPlanAssociations = l.ActionPlanAssociations.Where(asss => asss.ThisActionPlanStatus == Core.Domain.ActionPlanAggregate.ActionPlanStatus.Running)
              .Select(ass => new ActionPlanAssociationDTO
              {
                  Id = ass.Id,
                  APID = (int)ass.ActionPlanId,
                  APStatus = ass.ThisActionPlanStatus.ToString(),
                  APName = ass.ActionPlan.Name,
              }),
          }).FirstOrDefaultAsync(l => l.LeadId == request.leadId && l.brokerId == request.BrokerId);
        }
        else
        {
            lead = await _appDbContext.Leads
          .Select(l => new AllahLeadDTO
          {
              LeadId = l.Id,
              verifyEmailAddress = l.verifyEmailAddress,
              brokerId = l.BrokerId,
              Budget = l.Budget,
              Emails = l.LeadEmails.Select(em => new LeadEmailDTO { email = em.EmailAddress, isMain = em.IsMain }).ToList(),
              EntryDate = l.EntryDate.UtcDateTime,
              LeadFirstName = l.LeadFirstName,
              LeadLastName = l.LeadLastName,
              language = l.Language.ToString(),
              LastNotifsViewedAt = l.LastNotifsViewedAt,
              leadSource = l.source,
              leadSourceDetails = l.SourceDetails,
              LeadStatus = l.LeadStatus.ToString(),
              leadType = l.leadType.ToString(),
              Note = l.Note,
              PhoneNumber = l.PhoneNumber,
              Tags = l.Tags.Select(t => new TagDTO { id = t.Id, name = t.TagName }),
              ActionPlanAssociations = l.ActionPlanAssociations.Where(asss => asss.ThisActionPlanStatus == Core.Domain.ActionPlanAggregate.ActionPlanStatus.Running)
              .Select(ass => new ActionPlanAssociationDTO
              {
                  Id = ass.Id,
                  APID = (int)ass.ActionPlanId,
                  APStatus = ass.ThisActionPlanStatus.ToString(),
                  APName = ass.ActionPlan.Name,
              }),
              LeadAppEvents = l.AppEvents
              .Where(e => e.BrokerId == request.BrokerId && e.NotifyBroker)
              .Select(e => new LeadAppEventAllahLeadDTO
              {
                  IsActionPlanResult = e.IsActionPlanResult,
                  EventTimeStamp = e.EventTimeStamp,
                  EventType = EnumExtensions.ConvertEnumFlagsToString(e.EventType),
                  NotifProps = e.Props,
                  ReadByBroker = e.ReadByBroker,
              }),
              leadEmailEvents = l.EmailEvents
              .Where(e => e.BrokerId == request.BrokerId && (!e.Seen || (e.NeedsAction && !e.RepliedTo)))
              .Select(e => new LeadEmailEventAllahLeadDTO
              {
                  EmailId  = e.Id,
                  NeedsAction = e.NeedsAction,
                  BrokerEmail = e.BrokerEmail,
                  RepliedTo = e.RepliedTo,
                  Seen = e.Seen
              })
          }).FirstOrDefaultAsync(l => l.LeadId == request.leadId && l.brokerId == request.BrokerId);
        }

        var first = lead.Emails.First(e => e.isMain);
        lead.Emails.Remove(first);
        lead.Emails.Insert(0, first);
        return lead;
    }
}