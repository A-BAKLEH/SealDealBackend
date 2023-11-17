namespace Web.Constants;

public static class NotificationJSONKeys
{
    /// <summary>
    /// Id of user who committed the action
    /// </summary>
    public const string UserId = "UserID";

    public const string EmailSent = "EmailSent";
    public const string TempPasswd = "TempPassword";

    public const string ListingId = "ListingID";
    public const string ListingAddress = "ListingAddress";

    public const string AdminNote = "AdminNote";

    public const string AssignedById = "AssignedById";
    public const string AssignedByFullName = "AssignedByFullName";

    public const string AssignedToId = "AssignedById";
    public const string AssignedToFullName = "AssignedToFullName";

    public const string CreatedByFullName = "CreatedByFullName";
    public const string CreatedById = "CreatedById ";
    /// <summary>
    /// id of email from which a lead was parsed
    /// </summary>
    public const string EmailId = "EmailId";

    public const string APTriggerType = "APTriggerType";
    public const string TriggeredManually = "Triggered Manually";

    public const string ActionPlanId = "ActionPlanId";
    public const string ActionPlanName = "ActionPlanName";
    public const string OldLeadStatus = "OldLeadStatus";
    public const string NewLeadStatus = "NewLeadStatus";
    public const string ActionId = "ActionId";
    public const string APAssID = "APAssID";

    public const string APFinishedReason = "APFinishedReason";
    public const string AllActionsCompleted = "AllActionsCompleted";
    public const string ActionPlanError = "Error Encountered";
    public const string LeadResponded = "LeadResponded";
    public const string CancelledByBroker = "CancelledByBroker";

    public const string ListingOfInterestAddress = "ListingOfInterestAddress";
    public const string ListingOfInterestID = "ListingOfInterestID";
    public const string MultipleMatchingListings = "MultipleMatchingListings";
    public const string NoMatchingListings = "NoMatchingListings";

    public const string SuggestedAssignToId = "SuggestedAssignToId";
    public const string suggestedAssignToFullName = "suggestedAssignToFullName";
}
