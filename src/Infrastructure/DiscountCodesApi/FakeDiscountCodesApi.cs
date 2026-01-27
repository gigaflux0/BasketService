using Application.Ports;

namespace DiscountCodesApi;

public class FakeDiscountCodesApi : IDiscountCodesApi
{
    private static readonly Dictionary<string, DiscountCodeResult> Codes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["HALF"] = new DiscountCodeResult(
                Code: "HALF",
                Type: "percentage",
                Value: 50m
            )
        };

    public Task<DiscountCodeResult?> GetDiscountAsync(string code, CancellationToken ct)
    {
        Codes.TryGetValue(code, out var result);
        return Task.FromResult(result);
    }
}