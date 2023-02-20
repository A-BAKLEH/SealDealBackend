using System;
using Core.Domain.ActionPlanAggregate.Actions;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.ExternalServiceInterfaces.ActionPlans;
using Infrastructure.Data;
using Web.Constants;
using Microsoft.EntityFrameworkCore;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Core.Domain.BrokerAggregate.Templates;

namespace Web.Processing.ActionPlans;

public class ActionExecuter : IActionExecuter
{
  private readonly AppDbContext _appDbContext;
  private readonly ADGraphWrapper _adGraphWrapper;

  public ActionExecuter(AppDbContext appDbContext, ADGraphWrapper adGraphWrapper)
  {
    _appDbContext = appDbContext;
    _adGraphWrapper = adGraphWrapper;
  }

  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// 
  /// 0) ActionPlanAssociation - ActionPlanAssociation
  /// 1) List<ActionBase> - actions
  /// 2) Guid - brokerId
  /// 3) DateTime - timeNow
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  public async Task<bool> ExecuteChangeLeadStatus(params Object[] pars)
  {
    var ActionPlanAssociation = (ActionPlanAssociation)pars[0];
    var actions = (List<ActionBase>)pars[1];
    var brokerId = (Guid)pars[2];
    var timeNow = (DateTime)pars[3];

    var lead = ActionPlanAssociation.lead;
    var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
    var CurrentAction = actions[0];
    var currentActionLevel = CurrentAction.ActionLevel;

    string NewStatusString = CurrentAction.ActionProperties[ChangeLeadStatus.NewLeadStatus];

    Enum.TryParse<LeadStatus>(NewStatusString, true, out var NewLeadStatus);
    if (lead.LeadStatus == NewLeadStatus) return false;
    var oldStatus = lead.LeadStatus;
    lead.LeadStatus = NewLeadStatus;

    var StatusChangeNotif = new Notification
    {
      LeadId = lead.Id,
      BrokerId = brokerId,
      EventTimeStamp = timeNow,
      NotifType = NotifType.LeadStatusChange,
      ReadByBroker = false,
      NotifyBroker = true,
      IsActionPlanResult = true,
      //ProcessingStatus NO NEED for now, if later needs to be handled by outbox then assign
    };
    StatusChangeNotif.NotifProps[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
    StatusChangeNotif.NotifProps[NotificationJSONKeys.ActionId] = CurrentAction.Id.ToString();
    StatusChangeNotif.NotifProps[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();

    StatusChangeNotif.NotifProps[NotificationJSONKeys.OldLeadStatus] = oldStatus.ToString();
    StatusChangeNotif.NotifProps[NotificationJSONKeys.NewLeadStatus] = lead.LeadStatus.ToString();
    _appDbContext.Notifications.Add(StatusChangeNotif);

    // TODO------------- signalR and push Notif

    return true;
  }

  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// 
  /// 0) ActionPlanAssociation - ActionPlanAssociation
  /// 1) List<ActionBase> - actions
  /// 2) Guid - brokerId
  /// 3) DateTime - timeNow
  /// 
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  /// <exception cref="NotImplementedException"></exception>
  public async Task<Tuple<bool, EmailTemplate?>> ExecuteSendEmail(params Object[] pars)
  {
    var ActionPlanAssociation = (ActionPlanAssociation)pars[0];
    var actions = (List<ActionBase>)pars[1];
    var brokerId = (Guid)pars[2];
    var timeNow = (DateTime)pars[3];

    var lead = ActionPlanAssociation.lead;
    var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
    var CurrentAction = actions[0];
    var currentActionLevel = CurrentAction.ActionLevel;

    var TemplateId = int.Parse(CurrentAction.ActionProperties[SendEmail.EmailTemplateId]);
    var broker = await _appDbContext.Brokers
      .Select(b => new { b.Id, b.ConnectedEmails, Templates = b.Templates.Where(t => t.Id == TemplateId) })
      .FirstAsync(b => b.Id == brokerId);

    var connEmail = broker.ConnectedEmails[0];
    var template = (EmailTemplate)broker.Templates.First();


    //TODO determine if already sent
    bool alreadySent = false;
    if (alreadySent) return Tuple.Create<bool,EmailTemplate?>(false,null);
    _adGraphWrapper.CreateClient(connEmail.tenantId);

    //TODO replace variables in template text

    var tag = ActionPlanAssociation.Id.ToString() + "x" + template.Id;
    var message = new Message
    {
      Subject = template.EmailTemplateSubject,
      Body = new ItemBody
      {
        ContentType = BodyType.Text,
        Content = template.templateText
      },
      ToRecipients = new List<Recipient>()
      {
        new Recipient
        {
          EmailAddress = new EmailAddress
          {
            Address = lead.Email
          }
        }
      },
      SingleValueExtendedProperties = new MessageSingleValueExtendedPropertiesCollectionPage()
      {
        new SingleValueLegacyExtendedProperty
        {
          Id = APIConstants.APSentEmailExtendedPropId,
          Value = tag
        }
      }
    };

    await _adGraphWrapper._graphClient.Users[connEmail.Email]
        .SendMail(message, true)
        .Request()
        .PostAsync();

    template.TimesUsed++;
    var EmailSentNotif = new Notification
    {
      LeadId = lead.Id,
      BrokerId = brokerId,
      EventTimeStamp = timeNow,
      NotifType = NotifType.EmailEvent,
      ReadByBroker = false,
      NotifyBroker = true,
      IsActionPlanResult = true,
      IsRecevied= false,
      //ProcessingStatus NO NEED for now, if later needs to be handled by outbox then assign
    };
    EmailSentNotif.NotifProps[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
    EmailSentNotif.NotifProps[NotificationJSONKeys.ActionId] = CurrentAction.Id.ToString();
    EmailSentNotif.NotifProps[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();
    _appDbContext.Notifications.Add(EmailSentNotif);
    // TODO------------- signalR and push Notif

    return Tuple.Create(true, template);
  }
  /// <summary>
  /// Returns true if continue processing, false stop right away dont need to
  /// </summary>
  /// <param name="pars"></param>
  /// <returns></returns>
  /// <exception cref="NotImplementedException"></exception>
  public Task<bool> ExecuteSendSms(params Object[] pars)
  {
    throw new NotImplementedException();
  }
}
