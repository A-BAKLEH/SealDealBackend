using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Web.MediatrRequests.LeadRequests;

public class GetLeadEventsRequest : IRequest<List<NotifExpandedDTO>>
{
  public Guid BrokerId { get; set; }
  public int leadId { get; set; }
  public int lastNotifID { get; set; }
}
public class GetLeadEventsRequestHandler : IRequestHandler<GetLeadEventsRequest,List<NotifExpandedDTO>>
{
  private readonly AppDbContext _appDbContext;
  public GetLeadEventsRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  public async Task<List<NotifExpandedDTO>> Handle(GetLeadEventsRequest request, CancellationToken cancellationToken)
  {
    var notifs = await _appDbContext.Notifications
      .OrderByDescending(n => n.EventTimeStamp)
      //.Where(n =>  n.LeadId == request.leadId && n.Id < request.lastNotifID  )
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
    return notifs;
  }
}
