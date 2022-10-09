using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Web.ApiModels.APIResponses.Broker;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.ControllerServices.QuickServices;

public class BrokerTagsQService
{
  private readonly AppDbContext _appDbContext;
  public BrokerTagsQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  public async Task<BrokerTagsDTO>? GetBrokerTags(Guid brokerId)
  {
    var res = await _appDbContext.Tags.Where(t => t.BrokerId == brokerId).Select(t => new TagDTO
    {
      id = t.Id,
      name = t.TagName
    }).ToListAsync();
    if (res.Count() == 0) return null;
    return new BrokerTagsDTO { tags = res };
  }


}
