namespace Core.Config.Constants.LoggingConstants;
public static class TagConstants
{
    public const string msftWebhook = "msftWebhook";
    public const string Webhook = "webhook"; //for stripe

    public const string Unauthorized = "unauthorized";
    public const string Inactive = "inactive";

    public const string AgencySignup = "agencySignup";
    public const string CheckoutSession = "checkoutSession";
    public const string BillingPortal = "billingPortal";
    public const string AddBrokersRequest = "addBrokersRequest";

    public const string HangfireScheduleActionPlan = "hangfireScheduleActionPlan";
    public const string HangfireDispatch = "hangfireDispatch";
    public const string HangfireScheduleEmailParser= "HangfireScheduleEmailParser";

    public const string scheduleEmailParser = "scheduleEmailParser";

    public const string connectMsftEmail = "connectMsftEmail";
    public const string handleAdminConsentMsft = "handleAdminConsentMsft";

    public const string handlebrokerCreated = "handlebrokerCreated";
    public const string handleLeadAssigned = "handleLeadAssigned";
    public const string handleListingAssigned = "handleListingAssigned";

    public const string doAction = "doAction";
    public const string tagFailedMessages = "tagFailedMessages";
    public const string getFailedMessages = "getFailedMessages";

    public const string syncEmail = "syncEmail";

    public const string openAi = "openAi";
    public const string handleTaskResult = "handleTaskResult";
    public const string createDbRecords = "createDbRecords";

    public const string createDbRecordsResults = "createDbRecordsResults";
    public const string emailCategory = "emailCategory";
    public const string extendedValueAdding = "extendedValueAdding";

    public const string checkSeenAndRepliedTo = "checkSeenAndRepliedTo";
}
