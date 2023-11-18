using Core.Domain.ActionPlanAggregate;
using Core.Domain.AINurturingAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using MimeKit;
using Stripe;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Web.Constants;
using Web.HTTPClients;
using Web.Processing.Nurturing;
using static Google.Apis.Gmail.v1.UsersResource;

namespace Web.Processing.ActionPlans;

public class ActionExecuter
{
    private readonly AppDbContext _appDbContext;
    private readonly ADGraphWrapper _adGraphWrapper;
    private readonly ILogger<ActionExecuter> _logger;
    private GmailService? _GmailService;

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
    public Tuple<bool, AppEvent?> ExecuteChangeLeadStatus(ActionPlanAssociation ActionPlanAssociation, ActionExecutingDTO currentActionDTO, Guid brokerId, DateTime timeNow)
    {
        var lead = ActionPlanAssociation.lead;
        string NewStatusString = currentActionDTO.ActionProperties[ActionPlanAction.NewLeadStatus];

        Enum.TryParse<LeadStatus>(NewStatusString, true, out var NewLeadStatus);
        if (lead.LeadStatus == NewLeadStatus) return new Tuple<bool, AppEvent?>(false, null);
        var oldStatus = lead.LeadStatus;
        lead.LeadStatus = NewLeadStatus;

        var StatusChangeEvent = new AppEvent
        {
            LeadId = lead.Id,
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
            EventType = Core.Domain.NotificationAggregate.EventType.LeadStatusChange,
            ReadByBroker = false,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed,
        };
        StatusChangeEvent.Props[NotificationJSONKeys.ActionPlanName] = ActionPlanAssociation.ActionPlan.Name;
        StatusChangeEvent.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
        StatusChangeEvent.Props[NotificationJSONKeys.ActionId] = currentActionDTO.Id.ToString();
        StatusChangeEvent.Props[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();

        StatusChangeEvent.Props[NotificationJSONKeys.OldLeadStatus] = oldStatus.ToString();
        StatusChangeEvent.Props[NotificationJSONKeys.NewLeadStatus] = lead.LeadStatus.ToString();
        _appDbContext.AppEvents.Add(StatusChangeEvent);
        return new Tuple<bool, AppEvent?>(true, StatusChangeEvent);
    }

    public string ReplaceTemplateVariables(string input, Lead lead)
    {
        string pattern = @"\$(\w+)\$"; // Pattern to match words between $$ signs
        //string pattern = @"%(\w+)%";
        //MatchCollection matches = Regex.Matches(input, pattern);
        //var builder = new StringBuilder(input);
        var match = Regex.Match(input, pattern);
        while (match.Success)
        {
            string word = match.Groups[1].Value;
            //var wordWithoutDollards = word.Replace("%", "");
            var wordWithoutDollards = word.Replace("$", "");
            var index = match.Groups[1].Index;
            var replacementValue = "";
            switch (wordWithoutDollards)
            {
                case "firstname":
                    replacementValue = (string.IsNullOrEmpty(lead.LeadFirstName) || lead.LeadFirstName == "unknown") ? "" : lead.LeadFirstName;
                    break;
                case "lastname":
                    replacementValue = (string.IsNullOrEmpty(lead.LeadLastName) || lead.LeadLastName == "unknown") ? "" : lead.LeadLastName;
                    break;
                default:
                    break;
            }
            input = input.Remove(index - 1, word.Length + 2);
            input = input.Insert(index - 1, replacementValue);

            match = Regex.Match(input, pattern);
        }
        return input;
    }
    /// <summary>
    /// Returns true if continue processing, false stop right away dont need to
    /// might need signalR and push Notif after
    /// </summary>
    /// <param name="pars"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Tuple<bool, AppEvent?>> ExecuteSendEmail(ActionPlanAssociation ActionPlanAssociation, ActionExecutingDTO currentActionDTO, Guid brokerId, DateTime timeNow)
    {

        var lead = ActionPlanAssociation.lead;
        var CurrentActionTracker = ActionPlanAssociation.ActionTrackers[0];

        var TemplateId = currentActionDTO.dataTemplateId;
        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, b.ConnectedEmails, Templates = b.Templates.Where(t => t.Id == TemplateId) })
          .FirstOrDefaultAsync(b => b.Id == brokerId);
        if (broker == null)
        {
            _logger.LogError("{tag} no broker with Id {brokerId}", "ExecuteSendEmail", brokerId);
            return new Tuple<bool, AppEvent?>(false, null);
        }
        var connEmail = broker.ConnectedEmails.FirstOrDefault(e => e.isMailbox);
        if (connEmail == null)
        {
            _logger.LogError("{tag} no connectedEmail for broker with Id {brokerId}", "ExecuteSendEmail", brokerId);
            return new Tuple<bool, AppEvent?>(false, null);

        }
        var template = (EmailTemplate)broker.Templates.First();

        if (connEmail.isMSFT) _adGraphWrapper.CreateClient(connEmail.tenantId);
        else
        {
            GoogleCredential cred = GoogleCredential.FromAccessToken(connEmail.AccessToken);
            _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });
        }

        var leadLang = lead.Language;
        string templateTextToUse = "";
        string subjectTextToUse = "";
        if (leadLang == Language.English)
        {
            if (template.templateLanguage == Language.English)
            {
                templateTextToUse = template.templateText;
                subjectTextToUse = template.EmailTemplateSubject;
            }
            else
            {
                templateTextToUse = template.translatedText;
                subjectTextToUse = template.TranslatedEmailTemplateSubject;
            }
        }
        else
        {
            if (template.templateLanguage == Language.French)
            {
                templateTextToUse = template.templateText;
                subjectTextToUse = template.EmailTemplateSubject;
            }
            else
            {
                templateTextToUse = template.translatedText;
                subjectTextToUse = template.TranslatedEmailTemplateSubject;
            }
        }

        var replacedText = ReplaceTemplateVariables(templateTextToUse, lead);

        if (connEmail.isMSFT)
        {
            var tag = ActionPlanAssociation.Id.ToString() + "x" + template.Id;
            var message = new Microsoft.Graph.Models.Message
            {
                Subject = subjectTextToUse,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = replacedText
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
        }
        else
        {
            var labelsRes = await _GmailService.Users.Labels.List("me").ExecuteAsync();
            var labels = labelsRes.Labels.ToList();
            var sentBySealDeal = labels.FirstOrDefault(l => l.Name == "SealDeal:SentByWorkflow");
            if (sentBySealDeal == null)
            {
                sentBySealDeal = await _GmailService.Users.Labels.Create(new Label
                {
                    Name = "SealDeal:SentByWorkflow",
                    LabelListVisibility = "labelShow",
                    MessageListVisibility = "show"
                }, "me").ExecuteAsync();
            }
            var mailMessage = new System.Net.Mail.MailMessage
            {
                To = { lead.LeadEmails[0].EmailAddress },
                Subject = subjectTextToUse,
                Body = replacedText,
                IsBodyHtml = true
            };

            var mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);

            var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
            {
                Raw = Encode(mimeMessage),
            };
            var mes = await _GmailService.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
            await _GmailService.Users.Messages.Modify(new ModifyMessageRequest
            {
                AddLabelIds = new List<string>() { sentBySealDeal.Id },
            }, "me", mes.Id).ExecuteAsync();
        }

        template.TimesUsed++;
        var EmailSentNotif = new AppEvent
        {
            LeadId = lead.Id,
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
            EventType = Core.Domain.NotificationAggregate.EventType.ActionPlanEmailSent,
            ReadByBroker = false,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed
        };
        EmailSentNotif.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
        EmailSentNotif.Props[NotificationJSONKeys.ActionId] = currentActionDTO.Id.ToString();
        EmailSentNotif.Props[NotificationJSONKeys.ActionPlanName] = ActionPlanAssociation.ActionPlan.Name;
        EmailSentNotif.Props[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();
        _appDbContext.AppEvents.Add(EmailSentNotif);
        return new Tuple<bool, AppEvent?>(true, EmailSentNotif);
    }

    public static string EncodeReplyMessage(string replyText, string originalMessageRaw, string originalMessageId, string toEmail, string? subject, string fromEmail)
    {
        // Decode the original message (which is in base64url format)
        byte[] originalMessageBytes = Convert.FromBase64String(originalMessageRaw.Replace('-', '+').Replace('_', '/'));
        string originalMessage = Encoding.UTF8.GetString(originalMessageBytes);

        // Create a new MailMessage
        MailMessage mailMessage = new MailMessage();
        mailMessage.From = new MailAddress(fromEmail);
        mailMessage.To.Add(toEmail);
        mailMessage.Subject = "Re: " + subject;
        mailMessage.Body = replyText + "\n\n" + originalMessage;
        mailMessage.Headers.Add("In-Reply-To", originalMessageId);
        mailMessage.Headers.Add("References", originalMessageId);
        mailMessage.Headers.Add("Message-ID", originalMessageId);

        // Convert MailMessage to MimeMessage
        MimeMessage mimeMessage = MimeMessage.CreateFromMailMessage(mailMessage);

        // Encode the MimeMessage to base64url format
        using (var memoryStream = new MemoryStream())
        {
            mimeMessage.WriteTo(memoryStream);
            byte[] messageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(messageBytes).Replace('+', '-').Replace('/', '_');
        }
    }

    private static string DecodeBase64String(string encodedString)
    {
        byte[] data = Convert.FromBase64String(encodedString.Replace("-", "+").Replace("_", "/"));
        return Encoding.UTF8.GetString(data);
    }

    public async Task<List<InputEmail>> FetchThreadHistory(Guid brokerId, string threadId)
    {
        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, b.ConnectedEmails })
          .FirstOrDefaultAsync(b => b.Id == brokerId);

        var connEmail = broker.ConnectedEmails.FirstOrDefault(e => e.isMailbox);

        GoogleCredential cred = GoogleCredential.FromAccessToken(connEmail.AccessToken);
        _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });


        var request = _GmailService.Users.Threads.Get("me", threadId);
        request.Format = ThreadsResource.GetRequest.FormatEnum.Full; // Request the full format to include the body
        var thread = request.Execute();
        List<InputEmail> messageContents = new List<InputEmail>();

        if (thread.Messages != null)
        {
            foreach (var message in thread.Messages)
            {
                var part = message.Payload.Parts?.FirstOrDefault(p => p.MimeType == "text/plain");
                if (part == null)
                {
                    part = message.Payload; 
                }

                if (part.Body?.Data != null)
                {
                    string decodedString = DecodeBase64String(part.Body.Data);
                    string newContent = ExtractNewContent(decodedString);

                    // Extract sender and time
                    string sender = message.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value;
                    string time = message.Payload.Headers.FirstOrDefault(h => h.Name == "Date")?.Value;

                    // Create an instance of EmailDetail and add it to the list
                    messageContents.Add(new InputEmail
                    {
                        Email = sender,
                        Date = DateTime.Parse(time),
                        Content = newContent,
                        Type = sender == connEmail.Email ? NurturningEmailType.AIMessage : NurturningEmailType.LeadMessage
                    });
                }
            }
        }

        return messageContents;
    }

    public async Task ReplyToEmailById(Guid brokerId, int leadId, string messageId, string replyBody)
    {
        var lead = await _appDbContext.Leads.Include(x => x.LeadEmails).FirstOrDefaultAsync(l => l.Id == leadId);

        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, b.ConnectedEmails })
          .FirstOrDefaultAsync(b => b.Id == brokerId);

        var connEmail = broker.ConnectedEmails.FirstOrDefault(e => e.isMailbox);

        GoogleCredential cred = GoogleCredential.FromAccessToken(connEmail.AccessToken);
        _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });

        var messageRequest = _GmailService.Users.Messages.Get("me", messageId);
        var message = messageRequest.Execute();

        string threadId = message.ThreadId;

        var request = _GmailService.Users.Threads.Get("me", threadId);
        request.Format = ThreadsResource.GetRequest.FormatEnum.Full; // Request the full format to include the body
        var thread = request.Execute();

        // Fetch the original message to get the threadId and references

        string references = message.Payload.Headers.FirstOrDefault(h => h.Name == "Message-ID")?.Value;

        // Create the reply message
        var mailMessage = new MailMessage
        {
            Subject = "Re: " + message.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value,
            Body = replyBody,
            From = new MailAddress(connEmail.Email),
            Headers = {
            { "In-Reply-To", references },
            { "References", references },
        },
            To = { lead.LeadEmails[0].EmailAddress }
        };

        var rawMessage = ConvertMailMessageToMime(mailMessage);

        // Create the Gmail API message
        var replyMessage = new Google.Apis.Gmail.v1.Data.Message
        {
            Raw = rawMessage,
            ThreadId = threadId
        };

        // Send the reply
        var sendRequest = _GmailService.Users.Messages.Send(replyMessage, "me");
        sendRequest.Execute();
    }

    private static string ExtractNewContent(string emailContent)
    {
        var lines = emailContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int quoteIndex = Array.FindIndex(lines, line => line.StartsWith("On ") && line.Contains("wrote:"));

        if (quoteIndex >= 0)
        {
            // Take lines up to the quote index
            return string.Join("\n", lines.Take(quoteIndex)).Trim();
        }
        else
        {
            // No quote found, return the entire content
            return emailContent.Trim();
        }
    }

    private static string ConvertMailMessageToMime(MailMessage mailMessage)
    {
        using (var memoryStream = new MemoryStream())
        {
            var boundary = "boundary_" + DateTime.Now.Ticks.ToString("x");
            var encoding = new UTF8Encoding();

            using (var writer = new StreamWriter(memoryStream, encoding))
            {
                // Write headers
                writer.WriteLine($"From: {mailMessage.From.Address}");
                writer.WriteLine($"To: {string.Join(", ", mailMessage.To.Select(to => to.Address))}");
                writer.WriteLine($"Subject: {mailMessage.Subject}");
                writer.WriteLine($"MIME-Version: 1.0");
                writer.WriteLine($"Content-Type: multipart/alternative; boundary=\"{boundary}\"");
                writer.WriteLine();

                // Write body
                writer.WriteLine($"--{boundary}");
                writer.WriteLine("Content-Type: text/plain; charset=UTF-8");
                writer.WriteLine();
                writer.WriteLine(mailMessage.Body);
                writer.WriteLine($"--{boundary}--");
            }

            return Convert.ToBase64String(memoryStream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
    }

    public async Task<NurturingEmailResponse> ExecuteSendNurturingEmail(Guid brokerId, int leadId, int nurturingId, string textMessage, string replyingToMessageId = null, string threadId = null, string subject = "")
    {
        var timeNow = DateTime.UtcNow;

        var lead = await _appDbContext.Leads.Include(x => x.LeadEmails).FirstOrDefaultAsync(l => l.Id == leadId);

        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, b.ConnectedEmails })
          .FirstOrDefaultAsync(b => b.Id == brokerId);

        if (broker == null)
        {
            _logger.LogError("{tag} no broker with Id {brokerId}", "ExecuteSendEmail", brokerId);
            return new NurturingEmailResponse(false);
        }

        var connEmail = broker.ConnectedEmails.FirstOrDefault(e => e.isMailbox);
        if (connEmail == null)
        {
            _logger.LogError("{tag} no connectedEmail for broker with Id {brokerId}", "ExecuteSendEmail", brokerId);
            return new NurturingEmailResponse(false);
        }

        if (connEmail.isMSFT) _adGraphWrapper.CreateClient(connEmail.tenantId);
        else
        {
            GoogleCredential cred = GoogleCredential.FromAccessToken(connEmail.AccessToken);
            _GmailService = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });
        }

        var leadLang = lead.Language;
        string subjectTextToUse = subject;

        if (connEmail.isMSFT)
        {
            var tag = nurturingId.ToString();
            var message = new Microsoft.Graph.Models.Message
            {
                Subject = subjectTextToUse,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = textMessage
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
        }
        else
        {
            var labelsRes = await _GmailService.Users.Labels.List("me").ExecuteAsync();
            var labels = labelsRes.Labels.ToList();
            var sentBySealDeal = labels.FirstOrDefault(l => l.Name == "SealDeal:SentByAI");
            if (sentBySealDeal == null)
            {
                sentBySealDeal = await _GmailService.Users.Labels.Create(new Label
                {
                    Name = "SealDeal:SentByAI",
                    LabelListVisibility = "labelShow",
                    MessageListVisibility = "show"
                }, "me").ExecuteAsync();
            }

            MailMessage mailMessage = new MailMessage();

            if (!String.IsNullOrEmpty(replyingToMessageId))
            {
                var messageRequest = _GmailService.Users.Messages.Get("me", replyingToMessageId);
                var message = messageRequest.Execute();

                threadId = message.ThreadId;

                var request = _GmailService.Users.Threads.Get("me", threadId);
                request.Format = ThreadsResource.GetRequest.FormatEnum.Full; 
                var thread = request.Execute();

                string references = message.Payload.Headers.FirstOrDefault(h => h.Name == "Message-ID")?.Value;

                mailMessage = new MailMessage
                {
                    Subject = "Re: " + message.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value,
                    Body = textMessage,
                    From = new MailAddress(connEmail.Email),
                    Headers = {
                        { "In-Reply-To", references },
                        { "References", references },
                    },
                    To = { lead.LeadEmails[0].EmailAddress }
                };
            }
            else
            {
                mailMessage = new MailMessage
                {
                    Subject = subjectTextToUse,
                    Body = textMessage,
                    From = new MailAddress(connEmail.Email),
                    To = { lead.LeadEmails[0].EmailAddress }
                };
            }
                
            var rawMessage = ConvertMailMessageToMime(mailMessage);
            var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
            {
                Raw = rawMessage,
            };

            if (!String.IsNullOrEmpty(threadId))
            {
                gmailMessage.ThreadId = threadId;
            }

            var sentMessage = await _GmailService.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
            await _GmailService.Users.Messages.Modify(new ModifyMessageRequest
            {
                AddLabelIds = new List<string>() { sentBySealDeal.Id },
            }, "me", sentMessage.Id).ExecuteAsync();

            if (sentMessage != null && !string.IsNullOrEmpty(sentMessage.Id))
            {
                return new NurturingEmailResponse(true, sentMessage.ThreadId);
            }
            else
            {
                _logger.LogError($"Couldn't send an email, as a part of AI nurturing - {nurturingId}");
                return new NurturingEmailResponse(false);
            }
        }

        //var EmailSentNotif = new AppEvent
        //{
        //    LeadId = lead.Id,
        //    BrokerId = aiNurturing.BrokerId,
        //    EventTimeStamp = timeNow,
        //    EventType = Core.Domain.NotificationAggregate.EventType.ActionPlanEmailSent,
        //    ReadByBroker = false,
        //    IsActionPlanResult = true,
        //    ProcessingStatus = ProcessingStatus.NoNeed
        //};

        //_appDbContext.AppEvents.Add(EmailSentNotif);
        return new NurturingEmailResponse(false);
    }

    public static string Encode(MimeMessage mimeMessage)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            mimeMessage.WriteTo(ms);
            return Convert.ToBase64String(ms.GetBuffer())
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
    /// <summary>
    /// Returns true if continue processing, false stop right away dont need to
    /// </summary>
    /// <param name="pars"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Tuple<bool, AppEvent?>> ExecuteSendSms(ActionPlanAssociation ActionPlanAssociation, ActionExecutingDTO currentActionDTO, Guid brokerId, DateTime timeNow)
    {
        var lead = ActionPlanAssociation.lead;

        var TemplateId = currentActionDTO.dataTemplateId;
        var broker = await _appDbContext.Brokers
          .Select(b => new { b.Id, Templates = b.Templates.Where(t => t.Id == TemplateId) })
          .FirstAsync(b => b.Id == brokerId);

        var template = (SmsTemplate)broker.Templates.First();
        var leadLang = lead.Language;
        string templateTextToUse = "";
        if (leadLang == Language.English)
        {
            if (template.templateLanguage == Language.English) templateTextToUse = template.templateText;
            else templateTextToUse = template.translatedText;
        }
        else
        {
            if (template.templateLanguage == Language.French) templateTextToUse = template.templateText;
            else templateTextToUse = template.translatedText;
        }

        var replacedText = ReplaceTemplateVariables(templateTextToUse, lead);
        // todo SEND
        template.TimesUsed++;
        var SmsSentNotif = new AppEvent
        {
            LeadId = lead.Id,
            BrokerId = brokerId,
            EventTimeStamp = timeNow,
            //EventType = Core.Domain.NotificationAggregate.EventType.SmsEvent,
            ReadByBroker = false,
            IsActionPlanResult = true,
            ProcessingStatus = ProcessingStatus.NoNeed
        };
        SmsSentNotif.Props[NotificationJSONKeys.ActionPlanId] = ActionPlanAssociation.ActionPlanId.ToString();
        SmsSentNotif.Props[NotificationJSONKeys.ActionId] = currentActionDTO.Id.ToString();
        SmsSentNotif.Props[NotificationJSONKeys.APAssID] = ActionPlanAssociation.Id.ToString();
        _appDbContext.AppEvents.Add(SmsSentNotif);
        return new Tuple<bool, AppEvent?>(true, SmsSentNotif);
    }
}
