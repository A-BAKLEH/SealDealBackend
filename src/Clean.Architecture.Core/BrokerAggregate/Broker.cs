using Clean.Architecture.Core.AgencyAggregate;
using Clean.Architecture.Core.LeadAggregate;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.SharedKernel.Interfaces;

namespace Clean.Architecture.Core.BrokerAggregate;

public class Broker : Entity<Guid>, IAggregateRoot
{

  public int AgencyId { get; set; }
  public Agency Agency { get; set; }
  public string FirstName { get; set; }

  public string LastName { get; set; }

  public Boolean isAdmin { get; set; }

  public Boolean AccountActive { get; set; }
  public string? PhoneNumber { get; set; }

  public string Email { get; set; }

  //public Boolean IsAdmin { get; set; }

  public DateTime Created { get; set; } = DateTime.UtcNow;

  public List<Lead> Leads { get; set; }

  public List<Listing> Listings { get; set; }

  public List<SmsTemplate> SmsTemplates { get; set; }

  public List<EmailTemplate> EmailTemplates { get; set; }

  public List<ToDoTask> Tasks { get; set; }

  public List<Tag> BrokerTags { get; set; }

 



}
