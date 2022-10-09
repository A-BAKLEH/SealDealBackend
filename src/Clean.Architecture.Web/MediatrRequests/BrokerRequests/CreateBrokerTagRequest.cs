using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.DTOs.ProcessingDTOs;
using Clean.Architecture.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Clean.Architecture.Web.MediatrRequests.BrokerRequests;

public class CreateBrokerTagRequest : IRequest<CreateTagResultDTO>
{
  public Guid BrokerId { get; set; }
  public string TagName { get; set; }
}

public class CreateBrokerTagRequestHandler : IRequestHandler<CreateBrokerTagRequest, CreateTagResultDTO>
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
  /// <returns>true if success, false if tag already exists with this name</returns>
  public async Task<CreateTagResultDTO> Handle(CreateBrokerTagRequest request, CancellationToken cancellationToken)
  {
    CreateTagResultDTO result = new();

    if (_appDbContext.Tags.Any(t => t.BrokerId == request.BrokerId && request.TagName == request.TagName))
    {
      result.Success = false;
      result.message = "tag already exists";
    }
    _appDbContext.Tags.Add(new Tag { BrokerId = request.BrokerId, TagName = request.TagName});
    await _appDbContext.SaveChangesAsync();
    result.Success = true;
    return result;
  }
}

