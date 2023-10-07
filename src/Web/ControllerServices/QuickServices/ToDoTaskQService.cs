using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.ControllerServices.QuickServices;

public class ToDoTaskQService
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<ToDoTaskQService> _logger;
    public ToDoTaskQService(AppDbContext appDbContext, ILogger<ToDoTaskQService> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task DeleteToDoAsync(int ToDoId, Guid brokerId)
    {
        var todoTask = await _appDbContext.ToDoTasks.
          AsNoTracking()
          .FirstAsync(t => t.Id == ToDoId && t.BrokerId == brokerId);

        //TODO delete from calendar when deleting 

        await _appDbContext.Database.ExecuteSqlRawAsync
          ($"DELETE FROM \"ToDoTasks\" WHERE \"Id\" = {ToDoId};");
    }
}
