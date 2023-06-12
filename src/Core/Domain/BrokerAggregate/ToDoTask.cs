
using Core.Domain.LeadAggregate;
using SharedKernel;
namespace Core.Domain.BrokerAggregate;

public class ToDoTask : Entity<int>
{
    /// <summary>
    /// description
    /// </summary>
    public string? Description { get; set; }
    public string TaskName { get; set; }

    /// <summary>
    /// can be true to set it for deletion, so its not automaticaly deleted in case broker wants
    /// to review
    /// </summary>
    public bool IsDone { get; set; } = false;
    public DateTime TaskDueDate { get; set; }
    public string? HangfireReminderId { get; set; }
    public Guid BrokerId { get; set; }
    public Broker Broker { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
}

