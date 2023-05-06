using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate.EmailConnection;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using SharedKernel.Exceptions;
using Web.Processing.EmailAutomation;

namespace Web.ControllerServices.QuickServices;

public class MSFTEmailQService
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<MSFTEmailQService> _logger;
    private readonly EmailProcessor _emailProcessor;
    public MSFTEmailQService(AppDbContext appDbContext, EmailProcessor emailProcessor, ADGraphWrapper aDGraph, IConfiguration config, ILogger<MSFTEmailQService> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _emailProcessor = emailProcessor;
    }

    public async Task<dynamic> DummyMethodHandleAdminConsentAsync( string tenantId, Guid brokerId)
    {
        var broker = await _appDbContext.Brokers
          .Include(b => b.Agency)
          .Include(b => b.ConnectedEmails.Where(e => e.tenantId == tenantId))
          .FirstAsync(b => b.Id == brokerId);
        foreach (var em in broker.ConnectedEmails)
        {
            em.isMSFT = true;
            em.hasAdminConsent = true;
            em.tenantId = tenantId;
        }
        broker.Agency.AzureTenantID = tenantId;
        broker.Agency.HasAdminEmailConsent = true;
        await _appDbContext.SaveChangesAsync();
        return broker.ConnectedEmails.Select(e => new { e.Email, e.hasAdminConsent,e.isMSFT});
    }
    public async Task<dynamic> GetConnectedEmails(Guid brokerId)
    {
        var connectedEmails = await _appDbContext.ConnectedEmails
          .Select(e => new { e.BrokerId, e.hasAdminConsent, e.Email })
          .Where(c => c.BrokerId == brokerId)
          .ToListAsync();
        return (dynamic)connectedEmails;
    }
    /// <summary>
    /// Test if has access to tenant with this email and if yes subscribe to mailbox notifs
    /// AND checks and creates categories for emails
    /// Will thorw error if email already connected OR if no admin consent present
    /// ONLY MICROSOFT SUPPORTED FOR NOW
    /// </summary>
    public async Task<dynamic> ConnectEmail(Guid brokerId, string email, string TenantId)
    {

        var broker = _appDbContext.Brokers
          .Include(b => b.Agency)
          .Include(b => b.ConnectedEmails)
          .First(b => b.Id == brokerId);

        var connectedEmails = broker.ConnectedEmails;

        int emailNumber = 1;
        if (connectedEmails != null && connectedEmails.Count != 0)
        {
            foreach (var ConnEmail in connectedEmails)
            {
                if (ConnEmail.Email == email)
                    throw new
                      CustomBadRequestException($"the email {email} is already connected", ProblemDetailsTitles.EmailAlreadyConnected);

            }
            emailNumber = connectedEmails.Count + 1;
        }

        var connectedEmail = new ConnectedEmail
        {
            BrokerId = broker.Id,
            Email = email,
            EmailNumber = (byte) emailNumber,
            tenantId = TenantId,
            hasAdminConsent = broker.Agency.HasAdminEmailConsent,
            isMSFT = true,
        };

        if (broker.ConnectedEmails == null) broker.ConnectedEmails = new();
        broker.ConnectedEmails.Add(connectedEmail);
        if (!broker.Agency.HasAdminEmailConsent)
        {
            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            try
            {
                await _emailProcessor.CreateEmailSubscriptionAsync(connectedEmail);
                await _emailProcessor.CreateOutlookEmailCategoriesAsync(connectedEmail);
            }
            catch (ServiceException ex)
            {
                if (ex.ResponseStatusCode == 403)
                {
                    _logger.LogError("Connect Email: agency hadAdminConsent true so tried to create subsription but forbidden");
                    broker.Agency.HasAdminEmailConsent = false;
                    connectedEmail.hasAdminConsent = false;
                    BackgroundJob.Enqueue<EmailProcessor>(s => s.HandleAdminConsentConflict(broker.Id, connectedEmail.Email));
                    await _appDbContext.SaveChangesAsync();
                }
            }
        }
        return new { connectedEmail.Email, connectedEmail.hasAdminConsent };
    }
    /// <summary>
    /// To be primarily called by hangfire after a notif comes in
    /// For syncing
    /// </summary>
    /// <param name="ConnectedEmailId"></param>
    /// <param name="TenantId"></param>
    /// <returns></returns>
    public async Task SyncEmail(Guid subsId, string tenantId)
    {
        /*var connEmail = _appDbContext.ConnectedEmails.Single(e => e.GraphSubscriptionId == subsId);


        if(string.IsNullOrWhiteSpace(connEmail.FolderSyncToken))
        {

        }
        _adGraphWrapper.CreateClient(tenantId);

        List<Option> options = new List<Option>();

        options.Add(new QueryOption("$select", "sender,isRead,conversationId,conversationIndex,createdDateTime"));
        //options.Add(new QueryOption("$filter", "receivedDateTime+ge+{value}"));
        options.Add(new QueryOption("changeType", "created"));
        options.Add(new QueryOption("$orderby", "receivedDateTime+desc"));
        options.Add(new HeaderOption("Prefer: odata.maxpagesize", "30"));
        options.Add(new HeaderOption("Prefer: Prefer: IdType", "ImmutableId"));

        IMessageDeltaCollectionPage messages = await _adGraphWrapper._graphClient.Users[connEmail.Email]
          .MailFolders["inbox"].Messages.Delta()
          .Request(options).GetAsync();

        ProcessMessages(messages);

        while (messages.NextPageRequest != null)
        {
          //verify that header 30 max size exists
          messages = messages.NextPageRequest.GetAsync().Result;
          ProcessMessages(messages);
        }

        object? deltaLink;

        if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
        {
          connEmail.FolderSyncToken = deltaLink.ToString();
        }*/
    }



    /*public async Task HandleFirstFolderSync(ConnectedEmail connectedEmail, DateTimeOffset SyncStartDate, string FolderName)
    {
      var GraphClient = _adGraphWrapper._graphClient;

      var messages = await GetFirstMessagesPage(connectedEmail.Email, FolderName, SyncStartDate, true);

      var inboxSync1 = new FolderSync
      {
        FolderName = FolderName
      };
      connectedEmail.FolderSyncs.Add(inboxSync1);
      EmailHelpers.ProcessMessages(messages, _logger);

      while (messages.NextPageRequest != null)
      {
        //verify that header 30 max size exists
        messages = messages.NextPageRequest.GetAsync().Result;
        EmailHelpers.ProcessMessages(messages, _logger);
      }
      object? deltaLink;
      if (messages.AdditionalData.TryGetValue("@odata.deltaLink", out deltaLink))
      {
        inboxSync1.DeltaToken = deltaLink.ToString();
      }
      else
      {
        //TODO log error
      }
    }*/

    /// <summary>
    /// for broker trying to refresh status: 
    /// check if admin consent has been granted on agency, if yes subscribe to webhook.
    /// return all broker's connected Emails with true if success cuz multiple can be affected if belong to same tenant
    /// if agency has no admin consent, return null with false
    /// </summary>
    /// <param name="broker"></param>
    /// <param name="email"></param>
    /// <param name="TenantId"></param>
    /// <returns></returns>
    public async Task<Tuple<dynamic, bool>> HandleAdminConsentedAsync(Guid brokerId, string email)
    {
        var broker = await _appDbContext.Brokers
          .Include(b => b.Agency)
          .Include(b => b.ConnectedEmails)
          .FirstAsync(b => b.Id == brokerId);

        var tenantId = broker.ConnectedEmails.First(e => e.Email == email).tenantId;

        try
        {
            foreach (var e in broker.ConnectedEmails)
            {
                if (e.tenantId == tenantId && e.GraphSubscriptionId == null)
                {
                    await _emailProcessor.CreateEmailSubscriptionAsync(e, false);
                    e.hasAdminConsent = true;
                }

            }
        }
        catch (ServiceException ex)
        {
            if (ex.ResponseStatusCode == 403)
            {
                if (broker.Agency.HasAdminEmailConsent)
                {
                    _logger.LogError("HandleAdminConsent: agency hadAdminConsent true so tried to create subsription but forbidden");
                    broker.Agency.HasAdminEmailConsent = false;
                    BackgroundJob.Enqueue<EmailProcessor>(s => s.HandleAdminConsentConflict(broker.Id, email));
                    await _appDbContext.SaveChangesAsync();
                }
                return new Tuple<dynamic, bool>(null, false);
            }
            else throw;
        }
        if (!broker.Agency.HasAdminEmailConsent) broker.Agency.HasAdminEmailConsent = true;
        if (broker.Agency.AzureTenantID == null) broker.Agency.AzureTenantID = tenantId;

        //TODO later maybe hangfire handle all broker emails in the tenant automatically when admin consent is confirmed
        // for any person

        //in case endpoint executed somehow while email(s) already had admin consent and graph subscription
        var written = await _appDbContext.SaveChangesAsync();
        if (written > 0)
        {
            var ReturnedEmails = broker.ConnectedEmails.Select(e => new { e.Email, e.hasAdminConsent });
            return new Tuple<dynamic, bool>(ReturnedEmails, true);
        }
        return new Tuple<dynamic, bool>(null, false);
    }
}
