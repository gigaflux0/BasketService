using Newtonsoft.Json;

namespace ProjectionWorker.Models;

public class BasketProjection
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    public string? DiscountCode { get; init; }
    public IReadOnlyList<BasketItemProjection> Items { get; init; } = [];
    public IReadOnlyDictionary<string, decimal> ShippingCosts { get; init; } = new Dictionary<string, decimal>();
}