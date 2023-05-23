using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Web.Constants;
using Web.Outbox.Config;

namespace Web.Outbox;

/// <summary>
/// Sends Email with temp password to Broker.
/// </summary>
public class BrokerCreated : EventBase
{
}
public class BrokerCreatedHandler : EventHandlerBase<BrokerCreated>
{
    private readonly ADGraphWrapper _aDGraphWrapper;
    public BrokerCreatedHandler(AppDbContext appDbContext, ADGraphWrapper aDGraphWrapper, ILogger<BrokerCreatedHandler> logger) : base(appDbContext, logger)
    {
        _aDGraphWrapper = aDGraphWrapper;
    }

    public override async Task Handle(BrokerCreated BrokerCreatedEvent, CancellationToken cancellationToken)
    {
        AppEvent? appEvent = null;
        try
        {
            //process
            appEvent = _context.AppEvents.Include(e => e.Broker).FirstOrDefault(x => x.Id == BrokerCreatedEvent.AppEventId);
            if (appEvent == null) { _logger.LogError("No appEvent with NotifId {NotifId}", BrokerCreatedEvent.AppEventId); return; }

            if (appEvent.ProcessingStatus != ProcessingStatus.Done)
            {
                try
                {
                    _aDGraphWrapper.CreateClient(APIConstants.SealDealTenantId);
                    var message = new Message
                    {
                        Subject = "Your Temporary SealDeal Password",
                        From = new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = "support@sealdeal.ca"
                            }
                        },
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Text,
                            Content = $"Welcome To Seal Deal!\nHere is your temporary password:\n{appEvent.Props[NotificationJSONKeys.TempPasswd]}\n"
                        },
                        ToRecipients = new List<Recipient>()
                        {
                            new Recipient
                            {
                                EmailAddress = new EmailAddress
                              {
                                Address = appEvent.Broker.LoginEmail
                              }
                            }
                        }
                    };

                    var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                    { Message = message, SaveToSentItems = true };
                    _aDGraphWrapper.CreateClient(APIConstants.SealDealTenantId);

                    await _aDGraphWrapper._graphClient.Users["support@sealdeal.ca"]
                    .SendMail.PostAsync(requestBody);

                    appEvent.Props.Remove(NotificationJSONKeys.TempPasswd);
                    _context.Entry(appEvent).Property(f => f.Props).IsModified = true;
                }
                catch (ODataError er)
                {
                    _logger.LogCritical("{place} cannot send email with temp password for appEvent with" +
                        "AppEventId {AppEventId} with graph api error code {errCode} and message {errorMessage}", "MailSender", appEvent.Id, er.Error.Code, er.Error.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("{place} cannot send email with temp password for appEvent with" +
                        "AppEventId {AppEventId} with exception message {errorMessage}", "MailSender", appEvent.Id, ex.Message);
                }
            }
            await this.FinishProcessing(appEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError("Handling BrokerCreated Failed for appEvent with appEventId {AppEventId} with error {error}", BrokerCreatedEvent.AppEventId, ex.Message);
            appEvent.ProcessingStatus = ProcessingStatus.Failed;
            await _context.SaveChangesAsync();
            throw;
        }
    }
}
