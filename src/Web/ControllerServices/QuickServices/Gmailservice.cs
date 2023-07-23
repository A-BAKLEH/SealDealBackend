using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task ConnectGmailAsync(Guid brokerId)
    {
        var broker = _appDbContext.Brokers
          .Include(b => b.Agency)
          .Include(b => b.ConnectedEmails)
          .First(b => b.Id == brokerId);

        var connectedEmails = broker.ConnectedEmails;
    }
}
