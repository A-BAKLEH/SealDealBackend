using Microsoft.Extensions.Caching.Distributed;

namespace Clean.Architecture.Web.Cache.Extensions;

public static class DistributedCaching
{
  public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default) where T : class
  {
    await distributedCache.SetAsync(key, value.ToByteArray<T>(), options, token);
  }
  public static async Task<T> GetAsync<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default) where T : class
  {
    var result = await distributedCache.GetAsync(key, token);
    return result.FromByteArray<T>();
  }
}
