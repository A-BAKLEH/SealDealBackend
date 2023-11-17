using static Web.Processing.EmailAutomation.EmailProcessor;

namespace Web.Processing.Nurturing
{
    public class NurturingEmailResponse
    {
        public bool Success { get; set; }
        public string ThreadId { get; set; }

        public NurturingEmailResponse(bool success)
        {
            Success = success;
        }

        public NurturingEmailResponse(bool success, string threadId)
        {
            Success = success;
            ThreadId = threadId;
        }
    }

    public class NurturingEmailEvent
    {
        public int NurturingId { get; set; }
        public GmailMessageDecoded DecodedEmail { get; set; }
    }

    public class NurturingResult
    {
        public NurturingFinalStatus FinalStatus { get; set; }
    }

    public class EmailDetail
    {
        public string Sender { get; set; }
        public string Time { get; set; }
        public string Content { get; set; }
    }
}
