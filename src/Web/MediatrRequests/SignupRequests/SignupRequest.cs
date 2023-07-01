using Core.Domain.AgencyAggregate;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.TasksAggregate;
using Core.DTOs;
using Hangfire;
using Infrastructure.Data;
using MediatR;
using NuGet.Protocol.Plugins;
using Web.ApiModels.APIResponses.Broker;
using Web.ControllerServices;
using Web.Processing.Analyzer;
using Web.Processing.Cleanup;

namespace Web.MediatrRequests.SignupRequests;
public class SignupRequest : IRequest<SignedInBrokerDTO>
{
    public string AgencyName { get; set; }
    public string givenName { get; set; }
    public string surName { get; set; }
    public string email { get; set; }
    public string TimeZoneId { get; set; }
    public Guid b2cId { get; set; }
}

public class SignupRequestHandler : IRequestHandler<SignupRequest, SignedInBrokerDTO>
{
    private readonly AppDbContext _appDbContext;
    private readonly AuthorizationService _authorizeService;
    private readonly ILogger<SignupRequestHandler> _logger;
    private readonly IWebHostEnvironment _webHostEnv;

    public SignupRequestHandler(AppDbContext appDbContext, IWebHostEnvironment hostEnvironment, AuthorizationService authorizeService, ILogger<SignupRequestHandler> logger)
    {
        _appDbContext = appDbContext;
        _authorizeService = authorizeService;
        _logger = logger;
        _webHostEnv = hostEnvironment;
    }

    /// <summary>
    /// flow only gets here if token is issued with "newUser" claim ,which means B2C account created but not 
    /// stored in our DB yet.
    /// if already stored in DB, log a warning and return account Status 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SignedInBrokerDTO> Handle(SignupRequest request, CancellationToken cancellationToken)
    {
        if (_appDbContext.Brokers.FirstOrDefault(b => b.Id == request.b2cId) != null)
        {
            //log warning 
            //throw new InconsistentStateException("SignupRequest-UserAlreadyInDatabase","broker with B2C ID already exists in Brokers table", request.b2cId.ToString());
            _logger.LogWarning("broker with B2C ID already exists in Brokers table");
            var accountWithStatus = await this._authorizeService.VerifyAccountAsync(request.b2cId, request.TimeZoneId);

            return accountWithStatus;
        }
        using var transaction = await _appDbContext.Database.BeginTransactionAsync();
        var broker = new Broker()
        {
            Id = request.b2cId,
            FirstName = request.givenName,
            LastName = request.surName,
            LoginEmail = request.email,
            isAdmin = true,
            AccountActive = false,
            TimeZoneId = request.TimeZoneId,
            isSolo = true,
            Language = Language.English
        };
        var agency = new Agency()
        {
            AgencyName = request.AgencyName,
            NumberOfBrokersInSubscription = 0,
            StripeSubscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription,
            NumberOfBrokersInDatabase = 1,
            AgencyBrokers = new List<Broker> { broker }
        };
        _appDbContext.Add(agency);
        await _appDbContext.SaveChangesAsync();

        if(_webHostEnv.IsProduction())
        {
            var recJobOptions = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };
            var HangfireAnalyzerId = broker.Id.ToString() + "Analyzer";
            Random rnd = new Random();
            var minute = rnd.Next(0,59);
            //analyzer every hour except between 2 and 3 montreal time
            RecurringJob.AddOrUpdate<NotifAnalyzer>(HangfireAnalyzerId, a => a.AnalyzeNotifsAsync(broker.Id,null,CancellationToken.None), $"{minute} 0-5,7-23 * * *", recJobOptions);
            var recTask = new BrokerNotifAnalyzerTask
            {
                HangfireTaskId = HangfireAnalyzerId,
                BrokerId = broker.Id
            };
            _appDbContext.Add(recTask);

            var HangfireCleanerId = broker.Id.ToString() + "Cleaner";
            minute = rnd.Next(1, 20);
            
            //2:01 to 2:20 AM montreal time CLEANUP
            RecurringJob.AddOrUpdate<ResourceCleaner>(HangfireCleanerId, a => a.CleanBrokerResourcesAsync(broker.Id, null, CancellationToken.None), $"{minute} 6 * * *", recJobOptions);

            var recTaskCleaner = new BrokerNotifAnalyzerTask
            {
                HangfireTaskId = HangfireCleanerId,
                BrokerId = broker.Id
            };
            _appDbContext.Add(recTaskCleaner);
            await _appDbContext.SaveChangesAsync();
        }
        await transaction.CommitAsync();

        var response = new SignedInBrokerDTO();
        response.AgencyId = broker.AgencyId;
        response.BrokerId = broker.Id;
        response.Created = broker.Created;
        response.FirstName = broker.FirstName;
        response.isAdmin = broker.isAdmin;
        response.LastName = broker.LastName;
        response.LoginEmail = broker.LoginEmail;
        response.PhoneNumber = broker.PhoneNumber;
        response.markEmailsRead = broker.MarkEmailsRead;
        response.SoloBroker = broker.isSolo;
        response.AccountStatus = new AccountStatusDTO
        {
            userAccountStatus = "inactive",
            subscriptionStatus = StripeSubscriptionStatus.NoStripeSubscription.ToString(),
            internalMessage = "justSignedUp"
        };
        response.BrokerLanguage = broker.Language.ToString();
        return response;
    }
}

