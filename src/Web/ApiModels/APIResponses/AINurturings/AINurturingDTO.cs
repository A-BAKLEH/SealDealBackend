using Web.ApiModels.APIResponses.ActionPlans;

namespace Web.ApiModels.APIResponses.AINurturings
{
    public class AINurturingDTO
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public LeadNameIdDTO Lead { get; set; }
        public DateTime TimeCreated { get; set; }
        public AINurturingStatus Status { get; set; }
        public NurturingFinalStatus AnalysisStatus { get; set; }
        public int QuestionsCount { get; set; }
        public int FollowUpCount { get; set; }
        public DateTime? LastFollowupDate { get; set; }
        public string GmailThreadId { get; set; }
        public DateTime? LastProcessedMessageTime { get; set; }
        public bool InitialMessageSent { get; set; }
    }
}
