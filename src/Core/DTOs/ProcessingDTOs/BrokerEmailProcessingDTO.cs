using Core.Domain.ActionPlanAggregate;
using Core.Domain.LeadAggregate;

namespace Core.DTOs.ProcessingDTOs
{
    public class BrokerEmailProcessingDTO
    {
        public int AgencyId { get; set; }
        public Guid Id { get; set; }
        public Language BrokerLanguge { get; set; }
        public string brokerFirstName { get; set; }
        public string brokerLastName { get; set; }
        public string BrokerEmail { get; set; }
        public string? phoneNumber { get; set; }
        public bool isMsft { get; set; }
        public string? accessToken { get; set; }     
        public bool AssignLeadsAuto { get; set; }
        public bool isAdmin { get; set; }
        public bool isSolo { get; set; }
        public bool SmsNotifsEnabled { get; set; }
        public List<ActionPlan> brokerStartActionPlans { get; set; }
    }
}
