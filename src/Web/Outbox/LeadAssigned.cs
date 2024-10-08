﻿using Core.Config.Constants.LoggingConstants;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.NotificationAggregate;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Web.Constants;
using Web.Outbox.Config;
using Web.Processing.ActionPlans;
using Web.RealTimeNotifs;
using Task = System.Threading.Tasks.Task;

namespace Web.Outbox;

/// <summary>
/// when admin assign lead to broker. Handle ActionPlan or whatever if needed
/// especially when lead is created in-request. Also run signalR to notify broker of new lead if needed
/// </summary>
public class LeadAssigned : EventBase
{
}
public class LeadAssignedHandler : EventHandlerBase<LeadAssigned>
{
    private readonly RealTimeNotifSender _realTimeNotif;
    private string ourPhoneNumber = "";
    public LeadAssignedHandler(AppDbContext appDbContext, RealTimeNotifSender realTimeNotifSender, IConfiguration config, ILogger<LeadAssignedHandler> logger) : base(appDbContext, logger)
    {
        _realTimeNotif = realTimeNotifSender;
        ourPhoneNumber = config.GetSection("Twilio")["ourPhoneNumber"] ?? "";
    }

    public override async Task Handle(LeadAssigned LeadAssignedEvent, CancellationToken cancellationToken)
    {
        AppEvent? appEvent = null;
        try
        {
            //process
            appEvent = _context.AppEvents
                .Include(e => e.lead)
                .ThenInclude(l => l.ActionPlanAssociations)
                .Include(e => e.lead)
                .ThenInclude(e => e.LeadEmails)
                .Include(e => e.Broker)
                .ThenInclude(b => b.ActionPlans.Where(ap => ap.isActive && ap.Triggers.HasFlag(EventType.LeadAssignedToYou)))
                .FirstOrDefault(x => x.Id == LeadAssignedEvent.AppEventId);
            if (appEvent == null) { _logger.LogError("No appEvent with Id {AppEventId}", LeadAssignedEvent.AppEventId); return; }

            List<AppEvent> appEvents = new();
            if (appEvent.ProcessingStatus != ProcessingStatus.Done)
            {

                //Handle possible actionPlan for broker on lead Assignment FOR NOW NO AUTOMATIC ACTION PLAN
                if (appEvent.Broker.ActionPlans.Any())
                {
                    var timeNow = DateTime.UtcNow;
                    var lead = appEvent.lead;
                    foreach (var ap in appEvent.Broker.ActionPlans)
                    {
                        if (!lead.ActionPlanAssociations.Any(apa => apa.ActionPlanId == ap.Id) && !lead.ActionPlanAssociations.Any(apa => apa.ThisActionPlanStatus == ActionPlanStatus.Running && !apa.TriggeredManually))
                        {
                            var FirstActionDelay = ap.FirstActionDelay;
                            var delays = FirstActionDelay?.Split(':');
                            TimeSpan timespan = TimeSpan.Zero;
                            if (delays != null)
                            {
                                if (int.TryParse(delays[0], out var days)) timespan += TimeSpan.FromDays(days);
                                if (int.TryParse(delays[1], out var hours)) timespan += TimeSpan.FromHours(hours);
                                if (int.TryParse(delays[2], out var minutes)) timespan += TimeSpan.FromMinutes(minutes);
                            }
                            var firstAction = await _context.Actions.Select(a => new { a.ActionLevel, a.Id, a.ActionPlanId })
                                .FirstAsync(a => a.ActionPlanId == ap.Id && a.ActionLevel == 1);
                            var actionTracker = new ActionTracker
                            {
                                TrackedActionId = firstAction.Id,
                                ActionStatus = ActionStatus.ScheduledToStart,
                                HangfireScheduledStartTime = timeNow + timespan,
                            };
                            var apAssociation = new ActionPlanAssociation
                            {
                                ActionPlanId = ap.Id,
                                ActionPlanTriggeredAt = timeNow,
                                ThisActionPlanStatus = ActionPlanStatus.Running,
                                ActionTrackers = new() { actionTracker },
                                currentTrackedActionId = firstAction.Id,
                            };
                            lead.ActionPlanAssociations.Add(apAssociation);

                            bool OldHasActionPlanToStop = lead.HasActionPlanToStop;
                            if (ap.StopPlanOnInteraction) lead.HasActionPlanToStop = true;
                            if (ap.EventsToListenTo != EventType.None)
                            {
                                lead.EventsForActionPlans |= ap.EventsToListenTo; //for now not used
                            }
                            var APStartedEvent = new AppEvent
                            {
                                BrokerId = appEvent.BrokerId,
                                EventTimeStamp = timeNow,
                                EventType = EventType.ActionPlanStarted,
                                IsActionPlanResult = true,
                                ReadByBroker = false,
                                ProcessingStatus = ProcessingStatus.NoNeed,
                            };
                            APStartedEvent.Props[NotificationJSONKeys.APTriggerType] = appEvent.EventType.ToString();
                            APStartedEvent.Props[NotificationJSONKeys.ActionPlanId] = ap.Id.ToString();
                            APStartedEvent.Props[NotificationJSONKeys.ActionPlanName] = ap.Name;
                            lead.AppEvents = new() { APStartedEvent };
                            appEvents.Add(APStartedEvent);
                            string HangfireJobId = "";
                            try
                            {
                                if (delays != null)
                                {
                                    HangfireJobId = BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(lead.Id, firstAction.Id, firstAction.ActionLevel, ap.Id, null, CancellationToken.None), timespan);
                                }
                                else
                                {
                                    HangfireJobId = BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(lead.Id, firstAction.Id, firstAction.ActionLevel, ap.Id, null, CancellationToken.None));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("{tag} Hangfire error scheduling ActionPlan processor" +
                                " for ActionPlan {actionPlanID} and Lead {leadID} with error {error}", TagConstants.HangfireScheduleActionPlan, ap.Id, lead.Id, ex.Message + " :" + ex.StackTrace);
                                lead.ActionPlanAssociations.Remove(apAssociation);
                                lead.AppEvents.Remove(APStartedEvent);
                                lead.HasActionPlanToStop = OldHasActionPlanToStop;
                            }
                            actionTracker.HangfireJobId = HangfireJobId;
                        }
                    }
                }
                appEvents.Add(appEvent);
                if (appEvent.Broker.SMSNotifsEnabled)
                {
                    try
                    {
                        var messageOptions = new CreateMessageOptions(new PhoneNumber(appEvent.Broker.PhoneNumber));
                        messageOptions.From = new PhoneNumber(ourPhoneNumber);
                        var senderfullName = appEvent.Props[NotificationJSONKeys.AssignedByFullName];

                        var body = senderfullName == null ? "Lead Assigned:\n" : $"{senderfullName} assigned you a lead:\n";
                        body += $"First name: {appEvent.lead.LeadFirstName}\n";
                        body += $"Last name: {appEvent.lead.LeadLastName}\n";
                        body += $"Phone number: {appEvent.lead.PhoneNumber}\n";
                        if (appEvent.lead.LeadEmails != null && appEvent.lead.LeadEmails.Any())
                        {
                            var leadEmail = appEvent.lead.LeadEmails.First().EmailAddress;
                            body += $"Email: {leadEmail}\n";
                        }
                        body += "Log into SealDeal to view all available lead info.";
                        messageOptions.Body = body;

                        var mess = await MessageResource.CreateAsync(messageOptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical("{tag} failure sending twilio notifs error: {error} ", "Twilio", ex.Message + ":\n" + ex.StackTrace);
                    }
                }
                //TODO notify broker now if he's online and send PushNotif
                await _realTimeNotif.SendRealTimeNotifsAsync(_logger, appEvent.BrokerId, true, true, null, appEvents, null); ;
            }
            await this.FinishProcessing(appEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError("{tag} Handling LeadAssigned Failed for appEvent with appEventId {appEventId} with error {error}", TagConstants.handleLeadAssigned, LeadAssignedEvent.AppEventId, ex.Message);
            appEvent.ProcessingStatus = ProcessingStatus.Failed;
            await _context.SaveChangesAsync();
            throw;
        }
    }
}
