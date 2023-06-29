using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace Web.Config;

public class UserIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public UserIdEnricher() : this(new HttpContextAccessor())
    {
    }
    public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string? id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if(id != null)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty
                (
                "UserId", id)
                );
        }
    }
}
