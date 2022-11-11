using Microsoft.AspNetCore.SignalR;

namespace Clean.Architecture.Web.SignalRInfra;

public class NotifsHub : Hub
{
  public Task BroadcastMessage(string name, string message) =>
            Clients.All.SendAsync("broadcastMessage", name, message);

  public Task Echo(string name, string message) =>
      Clients.Client(Context.ConnectionId)
             .SendAsync("echo", name, $"{message} (echo from server)");
}
