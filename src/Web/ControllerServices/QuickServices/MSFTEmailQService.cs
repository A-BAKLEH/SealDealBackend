using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate.EmailConnection;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models.ODataErrors;
using SharedKernel.Exceptions;
using Web.Processing.EmailAutomation;

namespace Web.ControllerServices.QuickServices;

public class MSFTEmailQService
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<MSFTEmailQService> _logger;
    private readonly EmailProcessor _emailProcessor;
    private ADGraphWrapper _aDGraphWrapper;
    public MSFTEmailQService(AppDbContext appDbContext, ADGraphWrapper aDGraphWrapper, EmailProcessor emailProcessor, ADGraphWrapper aDGraph, IConfiguration config, ILogger<MSFTEmailQService> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _emailProcessor = emailProcessor;
        _aDGraphWrapper = aDGraphWrapper;
    }

    public async Task DisconnectEmailMsftAsync(Guid brokerId, string email)
    {
        var connectedEmail = await _appDbContext.ConnectedEmails
            .FirstOrDefaultAsync(e => e.BrokerId == brokerId && e.Email == email && e.isMSFT);
        if (connectedEmail == null) throw new CustomBadRequestException(ProblemDetailsTitles.NotFound, $"Email {email} not connected to broker", 404);

        var ActionPlansRunning = await _appDbContext.ActionPlanAssociations
            .Where(a => a.lead.BrokerId == brokerId && a.ThisActionPlanStatus == Core.Domain.ActionPlanAggregate.ActionPlanStatus.Running)
            .AnyAsync();
        if (ActionPlansRunning) throw new CustomBadRequestException(ProblemDetailsTitles.ActionPlansActive, $"Cannot disconnect email {email} while action plans are running", 403);

        var broker = await _appDbContext.Brokers.Include(b => b.Agency)
            .FirstAsync(b => b.Id == brokerId);
        //TODO later change when we have more than solo brokers
        broker.Agency.HasAdminEmailConsent = false;
        broker.Agency.AzureTenantID = null;

        try //if admin consent not given
        {
            _aDGraphWrapper.CreateClient(connectedEmail.tenantId);
            await _aDGraphWrapper._graphClient.Subscriptions[connectedEmail.GraphSubscriptionId.ToString()].DeleteAsync();
        }
        catch (Exception ex)
        { }

        try
        {
            if (connectedEmail.SubsRenewalJobId != null) BackgroundJob.Delete(connectedEmail.SubsRenewalJobId);
            if (connectedEmail.SyncJobId != null) BackgroundJob.Delete(connectedEmail.SyncJobId);
        }
        catch (Exception ex)
        {

        }

        _appDbContext.Remove(connectedEmail);
        await _appDbContext.SaveChangesAsync();
    }
    public async Task<dynamic> GetConnectedEmails(Guid brokerId)
    {
        var connectedEmails = await _appDbContext.ConnectedEmails
          .Select(e => new { e.BrokerId, e.hasAdminConsent, e.Email, e.AssignLeadsAuto, e.isMSFT, e.AccessToken })
          .Where(c => c.BrokerId == brokerId)
          .ToListAsync();
        return (dynamic)connectedEmails;
    }

    public async Task SetConnectedEmailAutoAssign(Guid brokerId, string Email, bool AutoAssign)
    {
        byte autoAssign = AutoAssign ? (byte)1 : (byte)0;
        await _appDbContext.Database.ExecuteSqlRawAsync($"UPDATE [dbo].[ConnectedEmails] SET AssignLeadsAuto = {autoAssign}" +
            $" WHERE Email = '{Email}' AND BrokerId = '{brokerId}';");
    }

    /// <summary>
    /// Test if has access to tenant with this email and if yes subscribe to mailbox notifs
    /// AND checks and creates categories for emails
    /// Will thorw error if email already connected OR if no admin consent present
    /// ONLY MICROSOFT SUPPORTED FOR NOW
    /// </summary>
    public async Task<dynamic> ConnectEmail(Guid brokerId, string email, string TenantId, bool AssignAdminLeadsAuto)
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
            EmailNumber = (byte)emailNumber,
            tenantId = TenantId,
            hasAdminConsent = broker.Agency.HasAdminEmailConsent,
            isMSFT = true,
            AssignLeadsAuto = AssignAdminLeadsAuto
        };

        if (broker.ConnectedEmails == null) broker.ConnectedEmails = new();
        broker.ConnectedEmails.Add(connectedEmail);
        bool TenantHasAdminConsent = broker.Agency.HasAdminEmailConsent && broker.Agency.AzureTenantID == TenantId;
        if (!TenantHasAdminConsent)
        {
            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            try
            {
                await _emailProcessor.CreateEmailSubscriptionAsync(connectedEmail);//calls save dbContext
                await _emailProcessor.CreateOutlookEmailCategoriesAsync(connectedEmail);
            }
            catch (ODataError ex)
            {
                if (ex.ResponseStatusCode == 403)
                {
                    _logger.LogError("{tag} agency hadAdminConsent true so tried to create subscribtion but forbidden", TagConstants.connectMsftEmail);
                    broker.Agency.HasAdminEmailConsent = false;
                    connectedEmail.hasAdminConsent = false;
                    //BackgroundJob.Enqueue<EmailProcessor>(s => s.HandleAdminConsentConflict(broker.Id, connectedEmail.Email));
                    await _appDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{tag} agency hadAdminConsent true, failed connecting email unknown why. error: {error}", TagConstants.connectMsftEmail, ex.Message + " :" + ex.StackTrace);
                throw;
            }
        }
        return new { connectedEmail.Email, connectedEmail.hasAdminConsent, connectedEmail.AssignLeadsAuto };
    }
    public async Task<dynamic> DummyMethodHandleAdminConsentAsync(string tenantId, Guid brokerId, int AgencyId)
    {
        var brokers = await _appDbContext.Brokers
          .Include(b => b.ConnectedEmails.Where(e => e.tenantId == tenantId && e.isMSFT && !e.hasAdminConsent))
          .Where(b => b.AgencyId == AgencyId)
          .ToListAsync();
        var agency = await _appDbContext.Agencies.FirstAsync(a => a.Id == AgencyId);
        agency.AzureTenantID = tenantId;
        bool error = false;
        foreach (var b in brokers)
        {
            foreach (var em in b.ConnectedEmails)
            {
                try
                {
                    await _emailProcessor.CreateEmailSubscriptionAsync(em, false);
                    await _emailProcessor.CreateOutlookEmailCategoriesAsync(em);
                }
                catch (ODataError ex)
                {
                    if (ex.ResponseStatusCode == 403)
                    {
                        error = true;
                        _logger.LogError("{tag} agency hadAdminConsent true so tried to create subscribtion but forbidden for connectedEmail {connectedEmail}", TagConstants.handleAdminConsentMsft, em.Email);
                        //BackgroundJob.Enqueue<EmailProcessor>(s => s.HandleAdminConsentConflict(broker.Id, connectedEmail.Email));
                        //await _appDbContext.SaveChangesAsync();
                        goto LoopEnd;
                    }
                    else throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError("{tag} agency hadAdminConsent true, failed connecting email unknown why. error: {error}", TagConstants.handleAdminConsentMsft, ex.Message + " :" + ex.StackTrace);
                    throw;
                }
                em.hasAdminConsent = true;
            }
        }
    LoopEnd:
        if (error)
        {
            agency.HasAdminEmailConsent = false;
        }
        else
        {
            agency.HasAdminEmailConsent = true;
        }
        await _appDbContext.SaveChangesAsync();
        return brokers.First(b => b.Id == brokerId).ConnectedEmails.Select(e => new { e.Email, e.hasAdminConsent, e.isMSFT });
    }



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
    //public async Task<Tuple<dynamic, bool>> HandleAdminConsentedAsync(Guid brokerId, string email)
    //{
    //    //UNUSED for now
    //    var broker = await _appDbContext.Brokers
    //      .Include(b => b.Agency)
    //      .Include(b => b.ConnectedEmails)
    //      .FirstAsync(b => b.Id == brokerId);

    //    var tenantId = broker.ConnectedEmails.First(e => e.Email == email).tenantId;

    //    try
    //    {
    //        foreach (var e in broker.ConnectedEmails)
    //        {
    //            if (e.tenantId == tenantId && e.GraphSubscriptionId == null)
    //            {
    //                await _emailProcessor.CreateEmailSubscriptionAsync(e, false);
    //                e.hasAdminConsent = true;
    //            }

    //        }
    //    }
    //    catch (ServiceException ex)
    //    {
    //        if (ex.ResponseStatusCode == 403)
    //        {
    //            if (broker.Agency.HasAdminEmailConsent)
    //            {
    //                _logger.LogError("HandleAdminConsent: agency hadAdminConsent true so tried to create subsription but forbidden");
    //                broker.Agency.HasAdminEmailConsent = false;
    //                BackgroundJob.Enqueue<EmailProcessor>(s => s.HandleAdminConsentConflict(broker.Id, email));
    //                await _appDbContext.SaveChangesAsync();
    //            }
    //            return new Tuple<dynamic, bool>(null, false);
    //        }
    //        else throw;
    //    }
    //    if (!broker.Agency.HasAdminEmailConsent) broker.Agency.HasAdminEmailConsent = true;
    //    if (broker.Agency.AzureTenantID == null) broker.Agency.AzureTenantID = tenantId;

    //    //TODO later maybe hangfire handle all broker emails in the tenant automatically when admin consent is confirmed
    //    // for any person

    //    //in case endpoint executed somehow while email(s) already had admin consent and graph subscription
    //    var written = await _appDbContext.SaveChangesAsync();
    //    if (written > 0)
    //    {
    //        var ReturnedEmails = broker.ConnectedEmails.Select(e => new { e.Email, e.hasAdminConsent });
    //        return new Tuple<dynamic, bool>(ReturnedEmails, true);
    //    }
    //    return new Tuple<dynamic, bool>(null, false);
    //}
}
