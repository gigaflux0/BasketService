namespace Domain;

public class BasketItem(string productId, int quantity)
{
    public string ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;

    public void Increase(int quantity) => Quantity += quantity;
}