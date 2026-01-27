namespace Application.Ports;

public interface IDiscountCodesApi
{
    Task<DiscountCodeResult?> GetDiscountAsync(string code, CancellationToken ct);
}

public record DiscountCodeResult(
    string Code,
    string Type,
    decimal Value
);