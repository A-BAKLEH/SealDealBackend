﻿namespace Web.Constants;

public static class NotificationJSONKeys
{
  /// <summary>
  /// Id of user who committed the action
  /// </summary>
  public const string UserId = "UserID";

  public const string EmailSent = "EmailSent";
  public const string TempPasswd = "TempPassword";

  public const string ListingId = "ListingID";

  public const string AdminNote = "AdminNote";

  public const string AssignedById = "AssignedById";
  public const string AssignedByFullName = "AssignedByFullName";

  public const string AssignedToId = "AssignedById";
  public const string AssignedToFullName = "AssignedToFullName";

  public const string CreatedByFullName = "CreatedByFullName";

  public const string APTriggerType = "APTriggerType";
  public const string TriggeredManually = "Triggered Manually";

  public const string ActionPlanId = "ActionPlanId";
  public const string OldLeadStatus = "OldLeadStatus";
  public const string NewLeadStatus = "NewLeadStatus";
  public const string ActionId = "ActionId";
  public const string APAssID = "APAssID";


  public const string APFinishedReason = "APFinishedReason";
  public const string AllActionsCompleted = "AllActionsCompleted";
  public const string LeadResponded= "LeadResponded";
}
