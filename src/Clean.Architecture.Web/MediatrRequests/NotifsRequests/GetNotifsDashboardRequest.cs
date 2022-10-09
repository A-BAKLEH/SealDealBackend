using Clean.Architecture.Core.Domain.NotificationAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.APIResponses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.NotifsRequests;

public class GetNotifsDashboardRequest : IRequest<NotifsResponseDTO>
{
  public Guid BrokerId { get; set; }
  /// <summary>
  /// most recent Notif Id preivosly fetched
  /// </summary>
  public int? MsRecentNotifId { get; set; }
  /// <summary>
  /// least recent Notif Id preivosly fetched
  /// </summary>
  public int? LstRecentNotifId { get; set; }
}
public class GetNotifsDashboardRequestHandler : IRequestHandler<GetNotifsDashboardRequest,NotifsResponseDTO>
{
  private readonly AppDbContext _appDbContext;
  public GetNotifsDashboardRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  //TODO handle with paging
  public async Task<NotifsResponseDTO> Handle(GetNotifsDashboardRequest request, CancellationToken cancellationToken)
  {
    var NotifResponseDTO = new NotifsResponseDTO();

    int notifTypeFilter = (int)(NotifType.EmailReceived | NotifType.SmsReceived | NotifType.CallReceived | NotifType.LeadStatusChange);

    var NotifWrappersList = await _appDbContext.Notifications
        .Where(n => n.BrokerId == request.BrokerId && (notifTypeFilter & ((int)n.NotifType)) > 0)
        .GroupBy(n => n.LeadId)
        .Select(x => new WrapperNotifDashboard
        {
          leadId = (int)x.Key,
          notifs = x.OrderByDescending(n => n.UnderlyingEventTimeStamp)
            .Select(n => new NotifForDashboardDTO
            {
              NotifType = n.NotifType,
              ReadByBroker = n.ReadByBroker,
              UnderlyingEventTimeStamp = n.UnderlyingEventTimeStamp
            })
        })
        .ToListAsync();
    int leadsNumber = NotifWrappersList.Count;
    List<int> leadIds = new(leadsNumber);
    NotifWrappersList.ForEach(x => leadIds.Add(x.leadId));
    var leadDTOs = await _appDbContext.Leads.Include(l => l.Tags)
      .AsSplitQuery()
      .OrderBy(l => l.Id)
      .Where(l => leadIds.Contains(l.Id))
      .Select(x => new DashboardLeadDTO
      {
        fstName = x.LeadFirstName,
        id = x.Id,
        lstName = x.LeadLastName,
        stsCode = x.LeadStatus,
        tags = x.Tags.Select(t => t.TagName)
      }).ToListAsync();

    for (int i = 0; i < leadIds.Count; i++)
    {
      var leadDTO = leadDTOs[i];
      leadDTO.stsStr = leadDTO.stsCode.ToString();
      NotifWrappersList[i].leadDTO = leadDTO;
    }

    NotifResponseDTO.data = NotifWrappersList;
    return NotifResponseDTO;
  }
}

