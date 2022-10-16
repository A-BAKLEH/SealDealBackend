using Azure.Core;
using Clean.Architecture.Core.Domain.BrokerAggregate;
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

  public async Task<BrokerTagsDTO>? GetBrokerTagsAsync(Guid brokerId)
  {
    var res = await _appDbContext.Tags.Where(t => t.BrokerId == brokerId).Select(t => new TagDTO
    {
      id = t.Id,
      name = t.TagName
    }).ToListAsync();
    if (res.Count() == 0) return null;
    return new BrokerTagsDTO { tags = res };
  }

  public async Task<CreateTagResultDTO> CreateBrokerTagAsync(Guid brokerId, string tagName)
  {
    CreateTagResultDTO result = new();

    if (_appDbContext.Tags.Any(t => t.BrokerId == brokerId && t.TagName == tagName))
    {
      result.Success = false;
      result.message = "tag already exists";
    }
    var tag = new Tag { BrokerId = brokerId, TagName = tagName };
    _appDbContext.Tags.Add(tag);
    await _appDbContext.SaveChangesAsync();
    result.Success = true;
    result.tag = tag;
    return result;
  }
}
