using Microsoft.AspNetCore.SignalR;
using Web.ControllerServices;

namespace Web.RealTimeNotifs;

//[Authorize]
public class NotifsHub : Hub
{
    private readonly AuthorizationService _authorizationService;
    public NotifsHub(AuthorizationService authorizeService)
    {
        _authorizationService = authorizeService;
    }
    public async Task BroadcastMessage(string name, string message) =>
              await Clients.All.SendAsync("broadcastMessage", name, message);

    public async Task Echo(string name, string message) =>
        await Clients.Client(Context.ConnectionId)
               .SendAsync("echo", name, $"{message} (echo from server)");

    //public override Task OnConnectedAsync()
    //{
    //  var id = Guid.Parse(Context.User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    //  var brokerTuple = _authorizationService.AuthorizeUser(id, true).Result;
    //  Console.WriteLine("signalR user: "+brokerTuple.Item1.LoginEmail);
    //  return Task.CompletedTask;
    //}
}
