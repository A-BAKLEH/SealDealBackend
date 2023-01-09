using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
namespace Web.Cache.Extensions;

public static class DistributedCaching
{
  public static async Task<T?> GetFromCacheAsync<T>(this IDistributedCache distributedCache,string key, CancellationToken token = default) where T : class
  {
    var cachedResponse = await distributedCache.GetStringAsync(key, token);
    return cachedResponse == null ? null : JsonSerializer.Deserialize<T>(cachedResponse);
  }
  public static async Task SetCacheAsync<T>(this IDistributedCache distributedCache,string key, T value, DistributedCacheEntryOptions? options = null , CancellationToken token = default) where T : class
  {
    var response = JsonSerializer.Serialize(value);
    if(options == null) options = new DistributedCacheEntryOptions();
    await distributedCache.SetStringAsync(key, response, options, token);
  }

  public static bool TryGetValue<T>(this IDistributedCache distributedCache, string key, out T? value)
  {
    var val = distributedCache.GetString(key);
    value = default;
    if (val == null) return false;
    value = JsonSerializer.Deserialize<T>(val);
    return true;
  }
}
