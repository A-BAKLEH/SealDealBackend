using Hangfire.Server;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Processing.Cleanup;
public class ResourceCleaner
{
    private readonly AppDbContext appDb;
    public ResourceCleaner(AppDbContext appDbContext)
    {
        appDb = appDbContext;
    }
    public async Task CleanBrokerResourcesAsync(Guid brokerId, PerformContext performContext)
    {
        await appDb.Notifs
            .Where(n => n.BrokerId == brokerId && n.isSeen)
            .ExecuteDeleteAsync();
        await appDb.EmailEvents
            .Where(e => e.BrokerId == brokerId && e.Seen && (!e.NeedsAction || (e.NeedsAction && e.RepliedTo)))
            .ExecuteDeleteAsync();
    }
}
