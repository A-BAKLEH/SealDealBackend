using Core.Constants.ProblemDetailsTitles;
using Core.Domain.BrokerAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Core.ExternalServiceInterfaces;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;

namespace Web.ControllerServices.QuickServices;

public class BrokerQService
{
    private readonly AppDbContext _appDbContext;
    private readonly IB2CGraphService _b2CGraphService;
    private readonly IStripeSubscriptionService _stripeSubscriptionService;
    private readonly ILogger<BrokerQService> _logger;
    public BrokerQService(AppDbContext appDbContext, ILogger<BrokerQService> logger, IB2CGraphService b2CGraphService, IStripeSubscriptionService stripeSubscriptionService)
    {
        _appDbContext = appDbContext;
        _b2CGraphService = b2CGraphService;
        _stripeSubscriptionService = stripeSubscriptionService;
        _logger = logger;
    }

    public async Task SetTimeZoneAsync(Broker broker, string NewTimeZoneId)
    {
        broker.TimeZoneId = NewTimeZoneId;
        await _appDbContext.SaveChangesAsync();
    }

    public async Task SetaccountLanguage(Guid brokerId, string lang)
    {
        Enum.TryParse<Language>(lang, true, out var language);
        var broker = await _appDbContext.Brokers.FirstOrDefaultAsync(b => b.Id == brokerId);
        broker.Language = language;
        await _appDbContext.SaveChangesAsync();
    }
    public async Task<List<BrokerForListDTO>> GetBrokersByAdmin(int AgencyId)
    {
        var brokers = await _appDbContext.Brokers
          .OrderByDescending(b => b.Created)
          .Where(b => b.AgencyId == AgencyId)
          .Select(b => new BrokerForListDTO
          {
              AccountActive = b.AccountActive,
              FirstName = b.FirstName,
              Id = b.Id,
              LastName = b.LastName,
              SigninEmail = b.LoginEmail,
              created = b.Created,
              PhoneNumber = b.PhoneNumber
          })
          .ToListAsync();
        return brokers;
    }
    //public async Task<List<BrokerListingDTO>> GetBrokersListings(Guid brokerId)
    //{

    //  var listings = await _appDbContext.BrokerListingAssignments
    //    .Where(b => b.BrokerId == brokerId)
    //    .OrderByDescending(a => a.assignmentDate)
    //    .Select(l => new BrokerListingDTO
    //    {
    //      ListingId = l.ListingId,
    //      Address = l.Listing.Address,
    //      DateOfListing = l.Listing.DateOfListing.UtcDateTime,
    //      ListingURL = l.Listing.URL,
    //      Price = l.Listing.Price,
    //      Status = l.Listing.Status.ToString(),
    //      DateAssignedToMe = l.assignmentDate,
    //      AssignedBrokersCount = l.Listing.BrokersAssigned.Count
    //    }).AsNoTracking().ToListAsync();

    //  return listings;
    //}
    public async Task DeleteBrokerAsync(Guid brokerDeleteId, Guid userId, int AgencyId)
    {
        //TODO create a deletion tracking object in a NoSql data store and update it after completion of each step here
        //try to retrieve it beginning of delete operation
        using var transaction = await _appDbContext.Database.BeginTransactionAsync();

        //check broker exists, belongs to this agency, is not an admin
        var agency = await _appDbContext.Agencies
          .Include(a => a.AgencyBrokers.Where(b => b.Id == brokerDeleteId && b.isAdmin == false))
          .ThenInclude(b => b.RecurrentTasks)
          .FirstAsync(a => a.Id == AgencyId);
        if (agency == null || agency.AgencyBrokers.First(b => b.Id == brokerDeleteId) == null
          || agency.AgencyBrokers.First(b => b.Id == brokerDeleteId).isAdmin) throw new CustomBadRequestException("invalid", ProblemDetailsTitles.InvalidInput);


        //delete from B2C
        await _b2CGraphService.DeleteB2CUserAsync(brokerDeleteId);

        //Remove 1 quantity from Stripe
        var newQuantity = await _stripeSubscriptionService.DecreaseSubscriptionQuantityAsync(agency.StripeSubscriptionId, agency.NumberOfBrokersInSubscription);

        //decrease database number of brokers in DB and Stripe
        agency.NumberOfBrokersInSubscription = newQuantity;
        agency.NumberOfBrokersInDatabase--;

        //send notif to admin about stripe quantity change
        var StripeNotif = new AppEvent
        {
            BrokerId = userId,
            EventTimeStamp = DateTime.UtcNow,
            EventType = EventType.StripeSubsChanged,
            ProcessingStatus = ProcessingStatus.NoNeed,
            ReadByBroker = true,
        };
        _appDbContext.AppEvents.Add(StripeNotif);


        var todoTasks = await _appDbContext.ToDoTasks.Where(t => t.BrokerId == brokerDeleteId && t.IsDone == false)
          .Select(t => t.HangfireReminderId)
          .ToListAsync();

        foreach (var jobId in todoTasks)
        {
            if (jobId != null) try
                {
                    BackgroundJob.Delete(jobId);
                }
                catch (Exception) { }
        }
        if (agency.AgencyBrokers[0].RecurrentTasks != null && agency.AgencyBrokers[0].RecurrentTasks.Any())
        {
            foreach (var task in agency.AgencyBrokers[0].RecurrentTasks)
            {
                try
                {
                    BackgroundJob.Delete(task.HangfireTaskId);
                }
                catch (Exception) { }
            }
        }
        await _appDbContext.Database.ExecuteSqlRawAsync
          ($"DELETE FROM \"ToDoTasks\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Notifs\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"AppEvents\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"EmailEvents\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Leads\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"BrokerListingAssignments\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"RecurrentTasks\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Brokers\" WHERE \"Id\" = '{brokerDeleteId}';");

        //delete template should be by cascade
        //delete tags dont know

        await _appDbContext.SaveChangesAsync();
        await transaction.CommitAsync();

    }

    public async Task DeleteSoloBrokerWithoutTouchingStripeAsync(Guid brokerDeleteId, int agencyId)
    {
        //TODO create a deletion tracking object in a NoSql data store and update it after completion of each step here
        //try to retrieve it beginning of delete operation
        using var transaction = await _appDbContext.Database.BeginTransactionAsync();

        //check broker exists, belongs to this agency, is not an admin
        var agency = await _appDbContext.Agencies
          .Include(a => a.AgencyBrokers)
          .ThenInclude(b => b.RecurrentTasks)
          .FirstAsync(a => a.Id == agencyId);

        //delete from B2C
        try
        {
            await _b2CGraphService.DeleteB2CUserAsync(brokerDeleteId);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error deleting broker from B2C");
        }      

        var todoTasks = await _appDbContext.ToDoTasks.Where(t => t.BrokerId == brokerDeleteId && t.IsDone == false)
          .Select(t => t.HangfireReminderId)
          .ToListAsync();

        foreach (var jobId in todoTasks)
        {
            if (jobId != null) try
                {
                    BackgroundJob.Delete(jobId);
                }
                catch (Exception) { }
        }
        if (agency.AgencyBrokers[0].RecurrentTasks != null && agency.AgencyBrokers[0].RecurrentTasks.Any())
        {
            foreach (var task in agency.AgencyBrokers[0].RecurrentTasks)
            {
                try
                {
                    BackgroundJob.Delete(task.HangfireTaskId);
                }
                catch (Exception) { }
            }
        }
        await _appDbContext.Database.ExecuteSqlRawAsync
          ($"DELETE FROM \"ToDoTasks\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Notifs\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"AppEvents\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"EmailEvents\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Leads\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"BrokerListingAssignments\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"RecurrentTasks\" WHERE \"BrokerId\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Brokers\" WHERE \"Id\" = '{brokerDeleteId}';" +
          $"DELETE FROM \"Agencies\" WHERE \"Id\" = '{agencyId}';");
        //delete template should be by cascade
        //delete tags dont know
        await _appDbContext.SaveChangesAsync();
        await transaction.CommitAsync();

    }
}
