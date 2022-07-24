using Clean.Architecture.SharedKernel;

namespace Clean.Architecture.Core.BrokerAggregate;

    public class EmailTemplate : Entity<int>
    {
        //public int? AgencyId { get; set; }
        //public Agency? Agency { get; set; }
        public string EmailTemplateSubject { get; set; }
        public string EmailTemplateText { get; set; }
        public Guid BrokerId { get; set; }
        public Broker Broker { get; set; }

    }

