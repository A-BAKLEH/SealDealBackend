using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate;
using Core.Domain.BrokerAggregate.EmailConnection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.Constants;

namespace Web.ControllerServices.QuickServices;

public class MyGmailQService
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<MyGmailQService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigurationSection _GmailSection;
    private readonly IWebHostEnvironment webHostEnvironment;
    public MyGmailQService(AppDbContext appDbContext, ILogger<MyGmailQService> logger,
        IConfiguration configuration
        , IHttpClientFactory httpClientFactory, IWebHostEnvironment hostEnvironment)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _GmailSection = configuration.GetSection("Gmail");
        webHostEnvironment = hostEnvironment;
    }

    public async Task DisconnectGmailAsync(Guid brokerId, string email)
    {
        var connectedEmail = await _appDbContext.ConnectedEmails
            .Include(e => e.Broker)
           .FirstOrDefaultAsync(e => e.BrokerId == brokerId && e.Email == email && !e.isMSFT);
        if (connectedEmail == null) throw new CustomBadRequestException(ProblemDetailsTitles.NotFound, $"Email {email} not connected to broker", 404);

        var ActionPlansRunning = await _appDbContext.ActionPlanAssociations
            .Where(a => a.lead.BrokerId == brokerId && a.ThisActionPlanStatus == Core.Domain.ActionPlanAggregate.ActionPlanStatus.Running)
            .AnyAsync();
        if (ActionPlansRunning) throw new CustomBadRequestException(ProblemDetailsTitles.ActionPlansActive, $"Cannot disconnect email {email} while action plans are running", 403);

        await CallUnwatch(connectedEmail.Email, connectedEmail.BrokerId);

        var jobIdRefresh = connectedEmail.TokenRefreshJobId;
        if (jobIdRefresh != null) BackgroundJob.Delete(jobIdRefresh);
        if (connectedEmail.SyncJobId != null) BackgroundJob.Delete(connectedEmail.SyncJobId);
        if (connectedEmail.SubsRenewalJobId != null) RecurringJob.RemoveIfExists(connectedEmail.SubsRenewalJobId);

        var clientSecrets = new ClientSecrets
        {
            ClientId = _GmailSection["ClientId"],
            ClientSecret = _GmailSection["ClientSecret"]
        };
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
        });

        await flow.RevokeTokenAsync(connectedEmail.Email, connectedEmail.RefreshToken, CancellationToken.None);
        connectedEmail.Broker.hasConnectedCalendar = false;
        connectedEmail.Broker.CalendarSyncEnabled = false;
        _appDbContext.Remove(connectedEmail);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task RefreshAccessTokenAsync(string email, Guid brokerId, PerformContext performContext, CancellationToken cancellationToken)
    {
        var connEmail = await _appDbContext.ConnectedEmails
          .Where(e => e.Email == email && e.BrokerId == brokerId)
          .FirstOrDefaultAsync();

        if (connEmail == null)
        {
            _logger.LogError("no connEmail in db");
            return;
        }
        if (connEmail.RefreshToken == null)
        {
            _logger.LogError("no refresh token in db");
            return;
        }
        var clientSecrets = new ClientSecrets
        {
            ClientId = _GmailSection["ClientId"],
            ClientSecret = _GmailSection["ClientSecret"]
        };
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
        });
        var re = await flow.RefreshTokenAsync("me", connEmail.RefreshToken, CancellationToken.None);

        connEmail.AccessToken = re.AccessToken;
        var refreshTime = TimeSpan.FromMinutes(55);
        string tokenRefreshJobId = BackgroundJob.Schedule<MyGmailQService>(s => s.RefreshAccessTokenAsync(email, brokerId, null, CancellationToken.None), refreshTime);
        connEmail.TokenRefreshJobId = tokenRefreshJobId;
        await _appDbContext.SaveChangesAsync();
    }
    public async Task CallWatch(string email, Guid brokerId)
    {
        var accessToken = await _appDbContext.ConnectedEmails
          .Where(e => e.Email == email && e.BrokerId == brokerId)
          .Select(e => e.AccessToken)
          .FirstOrDefaultAsync();

        var subSection = _GmailSection.GetSection("PubSub");
        var projectId = subSection["ProjectId"];
        var topicName = subSection["TopicName"];
        GoogleCredential cred = GoogleCredential.FromAccessToken(accessToken);
        GmailService service = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });

        var watchResult = await service.Users.Watch(new WatchRequest
        {
            LabelFilterBehavior = "INCLUDE",
            LabelIds = new[] { "INBOX" },
            TopicName = $"projects/{projectId}/topics/{topicName}"
        },
        "me")
            .ExecuteAsync();
    }

    public async Task CallUnwatch(string email, Guid brokerId)
    {
        var accessToken = await _appDbContext.ConnectedEmails
          .Where(e => e.Email == email && e.BrokerId == brokerId && !e.isMSFT)
          .Select(e => e.AccessToken)
          .FirstOrDefaultAsync();

        GoogleCredential cred = GoogleCredential.FromAccessToken(accessToken);
        GmailService service = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });

        var watchResult = await service.Users.Stop("me")
            .ExecuteAsync();
    }

    public async Task createLabelsAsync(string gmail, Guid brokerId, string? access_token = null)
    {
        if (access_token == null)
            access_token = await _appDbContext.ConnectedEmails
          .Where(e => e.Email == gmail && e.BrokerId == brokerId)
          .Select(e => e.AccessToken)
          .FirstOrDefaultAsync();

        GoogleCredential cred = GoogleCredential.FromAccessToken(access_token);
        GmailService service = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });
        var labels = await service.Users.Labels.List("me").ExecuteAsync();
        var labelsList = labels.Labels;
        var tasks = new List<Task<Label>>(3);
        if (!labelsList.Any(l => l.Name == "SealDealReprocess"))
        {
            var l1 = service.Users.Labels.Create(new Label
            {
                Name = "SealDealReprocess",
                LabelListVisibility = "labelHide",
                MessageListVisibility = "hide"
            }, "me").ExecuteAsync();
            tasks.Add(l1);
        }

        if (!labelsList.Any(l => l.Name == "SealDeal:LeadCreated"))
        {
            var l2 = service.Users.Labels.Create(new Label
            {
                Name = "SealDeal:LeadCreated",
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            }, "me").ExecuteAsync();
            tasks.Add(l2);
        }

        if (!labelsList.Any(l => l.Name == "SealDeal:SentByWorkflow"))
        {
            var l3 = service.Users.Labels.Create(new Label
            {
                Name = "SealDeal:SentByWorkflow",
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            }, "me").ExecuteAsync();
            tasks.Add(l3);
        }
        await Task.WhenAll(tasks);
    }

    public async Task AddGoogleCalendarAsync(Guid brokerId, string email, string refreshToken, string accessToken)
    {
        var broker = await _appDbContext.Brokers
            .Include(b => b.ConnectedEmails)
            .FirstAsync(b => b.Id == brokerId);

        if (broker.ConnectedEmails == null) broker.ConnectedEmails = new List<ConnectedEmail>(1);

        var existingEmail = broker.ConnectedEmails.FirstOrDefault(e => e.Email == email);
        if(existingEmail == null)
        {
            var connectedEmail = new ConnectedEmail
            {
                BrokerId = brokerId,
                Email = email,
                EmailNumber = (byte) (broker.ConnectedEmails.Count + 1),
                tenantId = "",
                hasAdminConsent = true,
                isMSFT = false,
                AssignLeadsAuto = true,
                RefreshToken = refreshToken,
                AccessToken = accessToken,
                isMailbox = false,
                isCalendar = true
            };
            
            var refreshTime = TimeSpan.FromMinutes(55);
            string tokenRefreshJobId = BackgroundJob.Schedule<MyGmailQService>(s => s.RefreshAccessTokenAsync(email, brokerId, null, CancellationToken.None), refreshTime);
            connectedEmail.TokenRefreshJobId = tokenRefreshJobId;
            _appDbContext.ConnectedEmails.Add(connectedEmail);
        }
        else
        {
            existingEmail.RefreshToken = refreshToken;
            existingEmail.AccessToken = accessToken;
            existingEmail.isCalendar = true;
        }
        broker.hasConnectedCalendar = true;
        broker.CalendarSyncEnabled = true;
        await _appDbContext.SaveChangesAsync();
    }

    public async Task ToggleCalendarSync(Guid brokerId, bool toggle)
    {

        var broker = await _appDbContext.Brokers
          .FirstAsync(b => b.Id == brokerId);
        broker.CalendarSyncEnabled = toggle;
        await _appDbContext.SaveChangesAsync();
    }

    public async Task ConnectGmailAsync(Guid brokerId, string email, string refreshToken, string accessToken)
    {
        var broker = await _appDbContext.Brokers
          .Include(b => b.Agency)
          .Include(b => b.ConnectedEmails)
          .FirstAsync(b => b.Id == brokerId);

        var connectedEmails = broker.ConnectedEmails;
        ConnectedEmail connectedEmail = null;

        if(connectedEmails != null && connectedEmails.Any(e => e.Email == email))
        {
            connectedEmail = connectedEmails.First(e => e.Email == email);
            connectedEmail.isMailbox = true;
        }
        else
        {
            var emailNumber = connectedEmails?.Count ?? 0;
            emailNumber += 1;
            connectedEmail = new ConnectedEmail
            {
                BrokerId = broker.Id,
                Email = email,
                EmailNumber = (byte)emailNumber,
                tenantId = "",
                hasAdminConsent = true,
                isMSFT = false,
                AssignLeadsAuto = true,
                RefreshToken = refreshToken,
                AccessToken = accessToken,
                isMailbox = true,
                isCalendar = false
            };

            if (broker.ConnectedEmails == null) broker.ConnectedEmails = new();
            broker.ConnectedEmails.Add(connectedEmail);

            //job to refresh access tokens
            var refreshTime = TimeSpan.FromMinutes(55);
            string tokenRefreshJobId = BackgroundJob.Schedule<MyGmailQService>(s => s.RefreshAccessTokenAsync(email, brokerId, null, CancellationToken.None), refreshTime);
            connectedEmail.TokenRefreshJobId = tokenRefreshJobId;
        }

        
        try
        {
            var subSection = _GmailSection.GetSection("PubSub");
            var projectId = subSection["ProjectId"];
            var topicName = subSection["TopicName"];
            GoogleCredential cred = GoogleCredential.FromAccessToken(accessToken);
            GmailService service = new GmailService(new BaseClientService.Initializer { HttpClientInitializer = cred });

            if (!webHostEnvironment.IsDevelopment() || GlobalControl.TestModeConnectGmailWebhook)
            {
                var watchResult = await service.Users.Watch(new WatchRequest
                {
                    LabelFilterBehavior = "INCLUDE",
                    LabelIds = new[] { "INBOX" },
                    TopicName = $"projects/{projectId}/topics/{topicName}"
                }, "me")
                .ExecuteAsync();
                connectedEmail.historyId = watchResult.HistoryId?.ToString();
            }

            await createLabelsAsync(email, brokerId, accessToken);

            //hangfire recurrrent job that calls watch once every day
            string WebhooksubscriptionRenewalJobId = email + "Watch";
            var recJobOptions = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };
            var TimeNow = DateTime.UtcNow;
            var hour = TimeNow.Hour;
            var minute = TimeNow.Minute;
            if (hour == 6 || hour == 7) hour = 1;

            if (!webHostEnvironment.IsDevelopment() || GlobalControl.TestModeConnectGmailWebhook)
            {
                RecurringJob.AddOrUpdate<MyGmailQService>(WebhooksubscriptionRenewalJobId,
                a => a.CallWatch(email, brokerId), $"{minute} {hour} * * *", recJobOptions);
                connectedEmail.SubsRenewalJobId = WebhooksubscriptionRenewalJobId;
            }

            //any other params that needs to be set in the future
            await _appDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("{tag} agency hadAdminConsent true, failed connecting email unknown why. error: {error}", TagConstants.connectMsftEmail, ex.Message + " :" + ex.StackTrace);
            throw;
        }
    }

    public async Task<string?> GetTokenGmailAsync(Guid id, string email)
    {
        var connEmail = await _appDbContext.ConnectedEmails
            .Select(e => new { e.BrokerId, e.Email, e.isMSFT, e.AccessToken })
            .FirstOrDefaultAsync(e => e.Email == email && e.BrokerId == id && !e.isMSFT);
        return connEmail?.AccessToken;
    }
}
