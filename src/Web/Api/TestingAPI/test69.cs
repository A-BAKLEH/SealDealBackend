using Core.Domain.NotificationAggregate;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Web.Outbox;
using Web.Outbox.Config;

namespace Web.Api.TestingAPI
{
    public class test69 : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        public test69(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpGet("test69Abdallah")]
        public async Task<IActionResult> test_ef_navigation()
        {
            var notif = new Notification
            {
                BrokerId = Guid.Parse("EA14ECF1-FCDA-43C4-9325-197A953D58FA"),
                DeleteAfterProcessing = false,
                IsActionPlanResult = false,
                EventTimeStamp = DateTime.UtcNow,
                IsRecevied = false,
                NotifType = NotifType.None,
                ReadByBroker = false,
                NotifyBroker = false
            };
            _appDbContext.Notifications.Add(notif);
            _appDbContext.SaveChanges();

            var notifId = notif.Id;
            var test = new testEvent { NotifId = notifId };
            var HangfireJobId = Hangfire.BackgroundJob.Enqueue<OutboxDispatcher>(x => x.Dispatch(test));
            return Ok();
        }
    }
}
