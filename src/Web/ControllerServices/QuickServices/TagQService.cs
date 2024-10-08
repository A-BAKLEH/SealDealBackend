﻿using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Web.ApiModels.APIResponses.Broker;
using Microsoft.EntityFrameworkCore;
using Azure.Core;
using Core.Constants.ProblemDetailsTitles;
using SharedKernel.Exceptions;

namespace Web.ControllerServices.QuickServices;
public class TagQService
{
  private readonly AppDbContext _appDbContext;
  public TagQService(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }
  public async Task TagLeadAsync(int LeadId, int TagId, Guid brokerId)
  {
    var tag = await _appDbContext.Tags
      .Include(t => t.Leads.Where(l => l.Id == LeadId))
      .FirstAsync(t => t.Id == TagId && t.BrokerId == brokerId);

    if (tag != null && !tag.Leads.Any())
    {
      await _appDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO \"LeadTag\" VALUES ({LeadId}, {TagId});");
    }
  }

  public async Task DeleteTagFromLeadAsync(int LeadId, int TagId, Guid brokerId)
  {
    await _appDbContext.Database.ExecuteSqlRawAsync($"DELETE FROM \"LeadTag\" WHERE \"LeadId\" = {LeadId} AND \"TagsId\" = {TagId};");
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

  public async Task<TagDTO> CreateBrokerTagAsync(Guid brokerId, string tagName)
  {
    CreateTagResultDTO result = new();

    if (_appDbContext.Tags.Any(t => t.BrokerId == brokerId && t.TagName == tagName))
    {
      result.Success = false;
      result.message = "tag already exists";
      throw new CustomBadRequestException("tag with name already exists", ProblemDetailsTitles.TagAlreadyExists);
    }
    var tagToAdd = new Tag { BrokerId = brokerId, TagName = tagName};
    _appDbContext.Tags.Add(tagToAdd);
    await _appDbContext.SaveChangesAsync();

    return new TagDTO { id = tagToAdd.Id, name = tagToAdd.TagName };
  }
}
