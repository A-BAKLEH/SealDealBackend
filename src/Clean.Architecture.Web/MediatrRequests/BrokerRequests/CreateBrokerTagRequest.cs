using Clean.Architecture.Core.Constants.ProblemDetailsTitles;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.SharedKernel.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;

public class CreateBrokerTagRequest : IRequest<TagDTO>
{
  public Guid BrokerId { get; set; }
  public string TagName { get; set; }
}

public class CreateBrokerTagRequestHandler : IRequestHandler<CreateBrokerTagRequest, TagDTO>
{
  private readonly AppDbContext _appDbContext;
  public CreateBrokerTagRequestHandler(AppDbContext appDbContext)
  {
    _appDbContext = appDbContext;
  }

  //TODO add cache
  //add TagName as index
  /// <summary>
  ///
  /// </summary>
  /// <param name="request"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<TagDTO> Handle(CreateBrokerTagRequest request, CancellationToken cancellationToken)
  {
    CreateTagResultDTO result = new();

    if (_appDbContext.Tags.Any(t => t.BrokerId == request.BrokerId && request.TagName == request.TagName))
    {
      result.Success = false;
      result.message = "tag already exists";
      throw new CustomBadRequestException("tag with name already exists", ProblemDetailsTitles.TagAlreadyExists);
    }
    var tagToAdd = new Tag { BrokerId = request.BrokerId, TagName = request.TagName };
    _appDbContext.Tags.Add(tagToAdd);
    await _appDbContext.SaveChangesAsync();
    
    return new TagDTO { id = tagToAdd.Id, name = tagToAdd.TagName};
  }
}

