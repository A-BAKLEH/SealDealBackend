using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Web.ControllerServices;

namespace Web.RealTimeNotifs;

[Authorize]
public class NotifsHub : Hub
{
    private readonly AuthorizationService _authorizationService;
    public NotifsHub(AuthorizationService authorizeService)
    {
        _authorizationService = authorizeService;
    }
}
