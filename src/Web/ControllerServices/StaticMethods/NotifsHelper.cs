using Core.Domain.NotificationAggregate;
using Web.Outbox;
using Web.Outbox.Config;

namespace Web.ControllerServices.StaticMethods;

public static class NotifsHelper
{
  //TODO implement
  public static EventBase? GetNotifEvent(NotifType notifType, int notifId) =>
    notifType switch
    {
      NotifType.None => throw new NotImplementedException(),
      NotifType.EmailEvent => throw new NotImplementedException(),
      NotifType.SmsEvent => throw new NotImplementedException(),
      NotifType.CallEvent => throw new NotImplementedException(),
      NotifType.CallMissed => throw new NotImplementedException(),
      NotifType.LeadStatusChange => throw new NotImplementedException(),
      NotifType.ListingAssigned => throw new NotImplementedException(),
      NotifType.ListingUnAssigned => throw new NotImplementedException(),
      NotifType.LeadCreated => new LeadCreated { NotifId = notifId },
      NotifType.LeadAssigned => throw new NotImplementedException(),
      NotifType.LeadUnAssigned => throw new NotImplementedException(),
      NotifType.ActionPlanStarted => throw new NotImplementedException(),
      NotifType.ActionPlanFinished => throw new NotImplementedException(),
      NotifType.BrokerCreated => throw new NotImplementedException(),
      NotifType.StripeSubsChanged => throw new NotImplementedException(),
      _ => null,
    };

  //{
  //  switch (notifType)
  //  {
  //    case NotifType.LeadCreated:
  //      return new LeadCreated { NotifId= notifId };
  //    default:

  //  }
  //}
}
