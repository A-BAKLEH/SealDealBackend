using System.Text.Json;

namespace Clean.Architecture.Web.Cache.Extensions;

public static class Serialization
{
  public static byte[] ToByteArray<T>(this T obj) where T : class
  {
    if (obj == null) return null;
    return JsonSerializer.SerializeToUtf8Bytes<T>(obj);
  }
  public static T FromByteArray<T>(this byte[] byteArray) where T : class
  {
    if (byteArray == null) return default;
    using (MemoryStream memoryStream = new MemoryStream(byteArray))
    {
      return JsonSerializer.Deserialize<T>(memoryStream);
    }
  }
}
