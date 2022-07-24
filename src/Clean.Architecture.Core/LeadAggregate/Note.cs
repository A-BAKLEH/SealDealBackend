namespace Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
public class Note : Entity<int>
{
  public string NotesText { get; set; }
  public int LeadId { get; set; }
  public Lead Lead { get; set; }

}

