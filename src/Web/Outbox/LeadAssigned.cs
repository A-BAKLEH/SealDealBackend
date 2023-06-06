using Core.Domain.ActionPlanAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Hangfire;
using Humanizer;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pipelines.Sockets.Unofficial.Arenas;
using Web.Constants;
using Web.Outbox.Config;
using Web.Processing.ActionPlans;
using Web.RealTimeNotifs;

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
    public LeadAssignedHandler(AppDbContext appDbContext, ILogger<LeadAssignedHandler> logger) : base(appDbContext, logger)
    {
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
                .Include(e => e.Broker)
                .ThenInclude(b => b.ActionPlans.Where(ap => ap.isActive && ap.Triggers.HasFlag(EventType.LeadAssignedToYou)))
                .FirstOrDefault(x => x.Id == LeadAssignedEvent.AppEventId);
            if (appEvent == null) { _logger.LogError("No appEvent with Id {AppEventId}", LeadAssignedEvent.AppEventId); return; }

            if (appEvent.ProcessingStatus != ProcessingStatus.Done)
            {
                //Handle possible actionPlan for broker on lead Assignment
                if(appEvent.Broker.ActionPlans.Any())
                {
                    var timeNow = DateTime.UtcNow;
                    var lead = appEvent.lead;
                    foreach (var ap in appEvent.Broker.ActionPlans)
                    {
                        if(!lead.ActionPlanAssociations.Any(apa => apa.ActionPlanId == ap.Id))
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
                            string HangfireJobId = "";
                            try
                            {
                                if (delays != null)
                                {
                                    HangfireJobId = BackgroundJob.Schedule<APProcessor>(p => p.DoActionAsync(lead.Id, firstAction.Id, firstAction.ActionLevel, ap.Id, null), timespan);
                                }
                                else
                                {
                                    HangfireJobId = BackgroundJob.Enqueue<APProcessor>(p => p.DoActionAsync(lead.Id, firstAction.Id, firstAction.ActionLevel, ap.Id, null));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogCritical("{place} Hangfire error scheduling ActionPlan processor" +
                                 " for ActionPlan {ActionPlanID} and Lead {LeadID} with error {Error}", "ScheduleActionPlanProcessor", ap.Id, lead.Id, ex.Message);
                                lead.ActionPlanAssociations.Remove(apAssociation);
                                lead.AppEvents.Remove(APStartedEvent);
                                lead.HasActionPlanToStop = OldHasActionPlanToStop;
                            }
                            actionTracker.HangfireJobId = HangfireJobId;
                        }
                    }
                }
                //TODO notify broker now if he's online and send PushNotif
                await RealTimeNotifSender.SendRealTimeNotifsAsync(_logger,appEvent.BrokerId,true, true, new List<AppEvent>(1) { appEvent }, null);
            }
            await this.FinishProcessing(appEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError("Handling ListingAssigned Failed for appEvent with appEventId {AppEventId} with error {error}", LeadAssignedEvent.AppEventId, ex.Message);
            appEvent.ProcessingStatus = ProcessingStatus.Failed;
            await _context.SaveChangesAsync();
            throw;
        }
    }
}
