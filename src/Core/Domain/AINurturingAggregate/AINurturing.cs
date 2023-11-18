using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using SharedKernel;

namespace Core.Domain.AINurturingAggregate
{
    public class AINurturing : Entity<int>
    {
        public bool IsActive { get; set; }
        public int LeadId { get; set; }
        public Lead lead  { get; set; }
        public Guid BrokerId { get; set; }
        public Broker broker { get; set; }
        public DateTime TimeCreated { get; set; }
        public AINurturingStatus Status { get; set; }
        public NurturingFinalStatus AnalysisStatus { get; set; }
        public int QuestionsCount { get; set; }
        public int FollowUpCount { get; set; }
        public DateTime? LastReplyDate { get; set; }
        public DateTime? LastFollowupDate { get; set; }
        public string ThreadId { get; set; }
        public DateTime? LastProcessedMessageTime { get; set; }
        public bool InitialMessageSent { get; set; }
    }
}

public enum AINurturingStatus
{ 
    Cancelled,
    StoppedNoResponse, 
    Running, 
    Done, 
}

public enum NurturingFinalStatus
{
    Hot,
    Cold,
    Slow,
    Active,
    Dead
}