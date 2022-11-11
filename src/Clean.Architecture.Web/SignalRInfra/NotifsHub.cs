using Microsoft.AspNetCore.SignalR;

namespace Clean.Architecture.Web.SignalRInfra;

public class NotifsHub : Hub
{
  public async Task BroadcastMessage(string name, string message) =>
            await Clients.All.SendAsync("broadcastMessage", name, message);

  public async Task Echo(string name, string message) =>
      await Clients.Client(Context.ConnectionId)
             .SendAsync("echo", name, $"{message} (echo from server)");
}
