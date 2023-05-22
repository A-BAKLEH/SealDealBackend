using Infrastructure.Data;

namespace Web.Processing.Analyzer;

public class NotifAnalyzer
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<NotifAnalyzer> _logger;
    public NotifAnalyzer(AppDbContext appDbContext, ILogger<NotifAnalyzer> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }


    public async Task AnalyzeNotifsAsync()
    {
        throw new NotImplementedException();
    }
}
