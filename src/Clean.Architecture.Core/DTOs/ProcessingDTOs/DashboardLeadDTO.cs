using Clean.Architecture.Core.Domain.LeadAggregate;

namespace Clean.Architecture.Core.DTOs.ProcessingDTOs;
public class DashboardLeadDTO
{
  public int id { get; set; }
  public string fstName { get; set; }
  public string lstName { get; set; }
  public IEnumerable<string>? tags { get; set; }
  public LeadStatus stsCode { get; set; }
  public string? stsStr { get; set; }
}
