using Newtonsoft.Json;

namespace Clean.Architecture.Web.ApiModels;

public class CheckoutSessionRequestDTO
{
  [JsonProperty("priceId")]
  public string PriceId { get; set; }

  [JsonProperty("quantity")]
  public int Quantity { get; set; }
}
