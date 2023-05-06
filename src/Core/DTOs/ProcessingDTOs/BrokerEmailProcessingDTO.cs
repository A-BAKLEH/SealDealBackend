using Core.Domain.LeadAggregate;

namespace Core.DTOs.ProcessingDTOs
{
    public class BrokerEmailProcessingDTO
    {
        public int AgencyId { get; set; }
        public Guid Id { get; set; }
        public Languge BrokerLanguge { get; set; }
        public string brokerFirstName { get; set; }
        public string brokerLastName { get; set; }
        public string BrokerEmail { get; set; }
        public bool AssignLeadsAuto { get; set; }
        public bool isAdmin { get; set; }
        public bool isSolo { get; set; }
    }
}
