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
  /// reminder is 1 or 2
  /// SignalR and Push Notif to Phone
  /// </summary>
  /// <param name="Id"></param>
  /// <returns></returns>
  public async Task Handle(int Id, int reminder)
  {
    //TODO check if reminder is 1 then schedule final reminder in 10 minutes
    //Console.WriteLine($"handling todoTask with id {Id} and reminder Position {reminder}");
  }
}
