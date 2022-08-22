using Microsoft.AspNetCore.Http;

namespace Clean.Architecture.Web.Config;

internal class CorrelationMiddleware
{
  internal const string CorrelationHeaderKey = "CorrelationId";

  private readonly RequestDelegate _next;

  public CorrelationMiddleware(
      RequestDelegate next)
  {
    this._next = next;
  }

  public async Task Invoke(HttpContext context)
  {
    var correlationId = Guid.NewGuid().ToString();

    if (context.Request != null)
    {
      context.Request.Headers.Add(CorrelationHeaderKey, correlationId);
    }
    /*context.Response.OnStarting(() =>
    {
      if (!context.Response.Headers.
      TryGetValue(CorrelationHeaderKey,
      out var correlationIds))
        context.Response.Headers.Add(CorrelationHeaderKey, correlationId);
      return Task.CompletedTask;
    });*/

    using (Serilog.Context.LogContext.PushProperty(CorrelationHeaderKey, correlationId))
    {
      await this._next.Invoke(context);
    }
  }
}
