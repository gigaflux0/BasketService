namespace Application.Basket.Commands.AddBasketItems;

public sealed record AddBasketItemsCommand(string BasketId, List<AddBasketItemsCommand.Item> Items)
{
    public record Item(string ProductId, int Quantity);
};
