using Clean.Architecture.Core.Domain.BrokerAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class CreateTagResultDTO
{
  public bool Success { get; set; }
  public string? message { get; set; }
  public Tag? tag { get; set; }
}
