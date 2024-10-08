﻿namespace Core.DTOs;
public class AccountStatusDTO
{
  public string subscriptionStatus { get; set; }
  public string userAccountStatus { get; set; }
  public string messageTodisplay { get; set; }
  public string internalMessage { get; set; } 
  public string routeUrl { get; set; }
  public bool TimeZoneChangeDetected { get; set; } =false;
  public string? MainTimeZone { get; set; }
  public string? DetectedTimeZone { get; set; }
}
