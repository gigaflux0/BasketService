namespace ProjectionWorker.Models;

public class BasketItemProjection
{
    public string ProductId { get; init; } = null!;
    public int Quantity { get; init; }
}