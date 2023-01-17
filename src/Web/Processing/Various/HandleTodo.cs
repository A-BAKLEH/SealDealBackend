using Infrastructure.Data;

namespace Web.Processing.Various;

public class HandleTodo
{
  private readonly AppDbContext _context;
  private readonly ILogger<HandleTodo> _logger; 
  public HandleTodo(AppDbContext appDbContext, ILogger<HandleTodo> logger)
  {
    _context = appDbContext;
    _logger = logger;
  }
  /// <summary>
  /// SignalR and Push Notif to Phone
  /// </summary>
  /// <param name="Id"></param>
  /// <returns></returns>
  public async Task Handle(int Id)
  {
    Console.WriteLine("handling todoTask");
  }
}
