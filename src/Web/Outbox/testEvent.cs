using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Web.Outbox.Config;

namespace Web.Outbox
{
    public class testEvent : EventBase
    {
    }
    public class testEventHandler : EventHandlerBase<testEvent>
    {
        public testEventHandler(AppDbContext appDbContext, ILogger<testEventHandler> logger) : base(appDbContext, logger)
        {
        }

        public override async Task Handle(testEvent notification, CancellationToken cancellationToken)
        {
            
            var notif = await _context.Notifications
                .Include(n => n.Broker)
                .FirstAsync(n => n.Id == notification.NotifId);
            _context.MessageWhenDisposed = "YES IT WORKS LMAO";
            _logger.LogWarning($"handling notif with id {notif.Id}");
        }
    }
}
