using Core.Domain.BrokerAggregate;
using Core.DTOs.ProcessingDTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Core.ExternalServiceInterfaces;
using Core.Domain.NotificationAggregate;
using Web.Constants;
using SharedKernel.Exceptions;
using Core.Constants.ProblemDetailsTitles;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Web.Outbox.Config;
using Web.Outbox;
using Hangfire;
using Microsoft.Graph;
using Hangfire.Common;
using Core.Domain.LeadAggregate;

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
        created = b.Created.UtcDateTime,
        LeadsCount = b.Leads.Count,
        ListingsCount = b.AssignedListings.Count,
        PhoneNumber = b.PhoneNumber
      })
      .ToListAsync();
    return brokers;
  }

  public async Task<List<BrokerListingDTO>> GetBrokersListings(Guid brokerId)
  {

    var listings = await _appDbContext.BrokerListingAssignments
      .Where(b => b.BrokerId == brokerId)
      .OrderByDescending(a => a.assignmentDate)
      .Select(l => new BrokerListingDTO
      {
        ListingId = l.ListingId,
        Address = l.Listing.Address,
        DateOfListing = l.Listing.DateOfListing.UtcDateTime,
        ListingURL = l.Listing.URL,
        Price = l.Listing.Price,
        Status = l.Listing.Status.ToString(),
        DateAssignedToMe = l.assignmentDate,
        AssignedBrokersCount = l.Listing.BrokersAssigned.Count
      }).AsNoTracking().ToListAsync();

    return listings;
  }
  public async Task DeleteBrokerAsync(Guid brokerDeleteId, Guid userId, int AgencyId)
  {
    //TODO create a deletion tracking object in a NoSql data store and update it after completion of each step here
    //try to retrieve it beginning of delete operation
    using var transaction = _appDbContext.Database.BeginTransaction();

    //check broker exists, belongs to this agency, is not an admin
    var agency = await _appDbContext.Agencies
      .Include(a => a.AgencyBrokers.Where(b => b.Id == brokerDeleteId && b.isAdmin == false ))
      .FirstAsync(a => a.Id == AgencyId);
    if (agency == null || agency.AgencyBrokers.First(b => b.Id == brokerDeleteId) == null) throw new CustomBadRequestException("invalid", ProblemDetailsTitles.InvalidInput);


    //delete from B2C
    await _b2CGraphService.DeleteB2CUserAsync(brokerDeleteId);

    //Remove 1 quantity from Stripe
    var newQuantity = await _stripeSubscriptionService.DecreaseSubscriptionQuantityAsync(agency.StripeSubscriptionId, agency.NumberOfBrokersInSubscription);

    //decrease database number of brokers in DB and Stripe
    agency.NumberOfBrokersInSubscription = newQuantity;
    agency.NumberOfBrokersInDatabase--;

    //send notif to admin about stripe quantity change
    var StripeNotif = new Notification
    {
      EventTimeStamp = DateTimeOffset.UtcNow,
      NotifType = NotifType.StripeSubsChanged,
      NotifyBroker = false,
      ProcessingStatus = ProcessingStatus.Scheduled,
      ReadByBroker = true,
    };
    StripeNotif.NotifProps.Add(NotificationJSONKeys.EmailSent, "0");
    _appDbContext.Notifications.Add(StripeNotif);
    //delete broker tadmir
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

    //TODO delete or by eventual consistency () make sure that hangfire jobs dont execute for
    // action plans, recurrentTasks, outbox events
    await _appDbContext.Database.ExecuteSqlRawAsync
      ($"DELETE FROM [dbo].[ToDoTasks] WHERE BrokerId = '{brokerDeleteId}';" +
      $"DELETE FROM [dbo].[Notifications] WHERE BrokerId = '{brokerDeleteId}';" +
      $"DELETE FROM [dbo].[Leads] WHERE BrokerId = '{brokerDeleteId}';" +
      $"DELETE FROM [dbo].[BrokerListingAssignments] WHERE BrokerId = '{brokerDeleteId}';" +
      $"DELETE FROM [dbo].[Brokers] WHERE Id = '{brokerDeleteId}';");

    
    //delete Leads -> delete notifs and todoTasks
    //deleting TodoTasks from broker will involve also related leads
    //deleting Notifs from broker will delete those related to leads ? but make sure they are always linked
    //delete lead will delete lead and action plan associations and action trackers by cascade

    //delete broker listing assignments
    //delete template should be by cascade
    //delete tags dont know
    //delete recurrent taskBase

    //delete action plans and their actions


    //Send email to admin to confirm Subscription Change
    await _appDbContext.SaveChangesAsync();
    var StripeNotifId = StripeNotif.Id;
    var stripeSubsChange = new StripeSubsChange { NotifId = StripeNotifId };
    try
    {
      var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(stripeSubsChange));
      OutboxMemCache.ScheduledDict.Add(StripeNotifId, HangfireJobId);
    }
    catch (Exception ex)
    {
      //TODO refactor log message
      _logger.LogCritical("Hangfire error scheduling Outbox Disptacher for BrokerCreated Event for notif" +
        "with {NotifId} with error {Error}", StripeNotifId, ex.Message);
      OutboxMemCache.SchedulingErrorDict.Add(StripeNotifId, stripeSubsChange);
    }
    transaction.Commit();

  }
}
