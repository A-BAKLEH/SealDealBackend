using SharedKernel;

namespace Core.Domain.LeadAggregate
{
    public class LeadEmail : EntityBase
    {
        public string EmailAddress { get; set; }
        public int LeadId { get; set; }
        public Lead Lead { get; set; }
        public bool IsMain { get; set; } = false;
    }
}
