using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate.EmailConnection;
using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using System.Net.Http.Headers;
using System.Text.Json;
using Web.ApiModels.RequestDTOs.Google;
using Web.Processing.EmailAutomation;

namespace Web.ControllerServices.QuickServices;
public class Gmailservice
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<Gmailservice> _logger;
    public Gmailservice(AppDbContext appDbContext, ILogger<Gmailservice> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task RefreshAccessTokenAsync(string email, Guid brokerId, PerformContext performContext,CancellationToken cancellationToken)
    {
        var refreshToken = await _appDbContext.ConnectedEmails
          .Where(e => e.Email == email && e.BrokerId == brokerId)
          .Select(e => e.GmailRefreshToken)
          .FirstOrDefaultAsync();
        if (refreshToken == null)
        {
            _logger.LogError("no refresh token in db");
            return;
        }
        var endpoint = "https://oauth2.googleapis.com/token";
        var data = new[]
            {
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("client_id", "912588585432-t1ui7blfmetvff3rmkjjjv19vf8pdouj.apps.googleusercontent.com"),
                    new KeyValuePair<string, string>("client_secret", "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"),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
            };

        var _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = null;
        try
        {
            response = await _httpClient.PostAsync("", new FormUrlEncodedContent(data));
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var dto1 = JsonSerializer.Deserialize<GoogleResDTO>(jsonResponse);
        }
        catch (Exception ex)
        {
            var ess = ex;
        }
    }

    public async Task ConnectGmailAsync(Guid brokerId, string email, string refreshToken, string accessToken)
    {
        var broker = await _appDbContext.Brokers
          .Include(b => b.Agency)
          .Include(b => b.ConnectedEmails)
          .FirstAsync(b => b.Id == brokerId);

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
            tenantId = "",
            hasAdminConsent = false,
            isMSFT = false,
            AssignLeadsAuto = true,
            GmailRefreshToken = refreshToken,
            GmailAccessToken = accessToken
        };

        if (broker.ConnectedEmails == null) broker.ConnectedEmails = new();
        broker.ConnectedEmails.Add(connectedEmail);
        try
        {
            //TODO await dummy function that sets webhook

            //TODO await dummy function that creates categories

            var refreshTime = TimeSpan.FromMinutes(55);
            string tokenRefreshJobId = BackgroundJob.Schedule<Gmailservice>(s => s.RefreshAccessTokenAsync(email,brokerId,null,CancellationToken.None), refreshTime);
            connectedEmail.GmailTokenRefreshJobId = tokenRefreshJobId;

            //TODO hangfire job that renews gmail subscription
            string WebhooksubscriptionRenewalJobId = "todo";
            connectedEmail.SubsRenewalJobId = WebhooksubscriptionRenewalJobId;
            //connectedEmail.GraphSubscriptionId have an ID for gmail subscription
            //connectedEmail.SubsExpiryDate

            //any other params that needs to be set
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
            .Select(e => new { e.BrokerId, e.Email, e.isMSFT, e.GmailAccessToken })
            .FirstOrDefaultAsync(e => e.Email == email && e.BrokerId == id && !e.isMSFT);
        return connEmail?.GmailAccessToken;
    }
}
