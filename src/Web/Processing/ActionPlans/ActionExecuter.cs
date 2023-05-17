﻿using Core.Domain.ActionPlanAggregate;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Web.Constants;

namespace Web.Processing.ActionPlans;

public class ActionExecuter
{
    private readonly AppDbContext _appDbContext;
    private readonly ADGraphWrapper _adGraphWrapper;
    private readonly ILogger<ActionExecuter> _logger;

    public ActionExecuter(AppDbContext appDbContext, ADGraphWrapper adGraphWrapper, ILogger<ActionExecuter> logger)
    {
        _appDbContext = appDbContext;
        _adGraphWrapper = adGraphWrapper;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if continue processing, false stop right away dont need to.
    /// // needs  signalR and push Notif after
    /// <param name="pars"></param>
    /// <returns></returns>
    public bool ExecuteChangeLeadStatus(ActionPlanAssociation ActionPlanAssociation, ActionExecutingDTO currentActionDTO, Guid brokerId, DateTime timeNow)
    {
        var lead = ActionPlanAssociation.lead;
        string NewStatusString = currentActionDTO.ActionProperties[ActionPlanAction.NewLeadStatus];

        Enum.TryParse<LeadStatus>(NewStatusString, true, out var NewLeadStatus);
        if (lead.LeadStatus == NewLeadStatus) return false;
        var oldStatus = lead.LeadStatus;
        lead.LeadStatus = NewLeadStatus;

        var StatusChangeEvent = new AppEvent
        {
            LeadId = lead.Id,
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
            EventType = Core.Domain.NotificationAggregate.EventType.LeadStatusChange,
            ReadByBroker = false,
            NotifyBroker = true,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed,
        };
        StatusChangeEvent.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
        StatusChangeEvent.Props[NotificationJSONKeys.ActionId] = currentActionDTO.Id.ToString();
        StatusChangeEvent.Props[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();

        StatusChangeEvent.Props[NotificationJSONKeys.OldLeadStatus] = oldStatus.ToString();
        StatusChangeEvent.Props[NotificationJSONKeys.NewLeadStatus] = lead.LeadStatus.ToString();
        _appDbContext.AppEvents.Add(StatusChangeEvent);
        return true;
    }

    /// <summary>
    /// Returns true if continue processing, false stop right away dont need to
    /// might need signalR and push Notif after
    /// </summary>
    /// <param name="pars"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> ExecuteSendEmail(ActionPlanAssociation ActionPlanAssociation, ActionExecutingDTO currentActionDTO, Guid brokerId, DateTime timeNow)
    {

        var lead = ActionPlanAssociation.lead;
        var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];
        //var CurrentAction = actions[0];
        //var currentActionLevel = CurrentAction.ActionLevel;

        var TemplateId = currentActionDTO.dataTemplateId;
        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, b.ConnectedEmails, Templates = b.Templates.Where(t => t.Id == TemplateId) })
          .FirstAsync(b => b.Id == brokerId);

        var connEmail = broker.ConnectedEmails[0];
        var template = (EmailTemplate)broker.Templates.First();

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
                  Address = lead.LeadEmails[0].EmailAddress
                }
              }
            },
            SingleValueExtendedProperties = new()
            {
              new SingleValueLegacyExtendedProperty
              {
                Id = APIConstants.APSentEmailExtendedPropId,
                Value = tag
              }
            },
            Categories = new List<string>()
            {
                  APIConstants.SentBySealDeal
            }
        };

        var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
        { Message = message, SaveToSentItems = true };

        await _adGraphWrapper._graphClient.Users[connEmail.Email]
            .SendMail.PostAsync(requestBody);

        template.TimesUsed++;
        var EmailSentNotif = new AppEvent
        {
            LeadId = lead.Id,
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
            EventType = Core.Domain.NotificationAggregate.EventType.ActionPlanEmailSent,
            ReadByBroker = false,
            NotifyBroker = true,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed
        };
        EmailSentNotif.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
        EmailSentNotif.Props[NotificationJSONKeys.ActionId] = currentActionDTO.Id.ToString();
        EmailSentNotif.Props[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();
        _appDbContext.AppEvents.Add(EmailSentNotif);
        return true;
    }
    /// <summary>
    /// Returns true if continue processing, false stop right away dont need to
    /// </summary>
    /// <param name="pars"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> ExecuteSendSms(ActionPlanAssociation ActionPlanAssociation, ActionExecutingDTO currentActionDTO, Guid brokerId, DateTime timeNow)
    {
        var lead = ActionPlanAssociation.lead;

        var TemplateId = currentActionDTO.dataTemplateId;
        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, Templates = b.Templates.Where(t => t.Id == TemplateId) })
          .FirstAsync(b => b.Id == brokerId);

        var template = (SmsTemplate)broker.Templates.First();

        // todo SEND
        template.TimesUsed++;
        var SmsSentNotif = new AppEvent
        {
            LeadId = lead.Id,
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
           //EventType = Core.Domain.NotificationAggregate.EventType.SmsEvent,
            ReadByBroker = false,
            NotifyBroker = true,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed
        };
        SmsSentNotif.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
        SmsSentNotif.Props[NotificationJSONKeys.ActionId] = currentActionDTO.Id.ToString();
        SmsSentNotif.Props[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();
        _appDbContext.AppEvents.Add(SmsSentNotif);
        return true;
    }
}
