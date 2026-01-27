using Domain.Events;

namespace Domain;

public class Basket
{
    private readonly List<BasketItem> _items = [];
    private readonly List<IBasketEvent> _pendingEvents = [];
    private readonly Dictionary<string, decimal> _shippingCosts = new();
    private string Id { get; set; } = null!;

    public IReadOnlyDictionary<string, decimal> ShippingCosts => _shippingCosts;
    public string? DiscountCode { get; private set; }
    public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<IBasketEvent> PendingEvents => _pendingEvents.AsReadOnly();

    private Basket() { }

    private Basket(string id)
    {
        Id = id;
        var created = new BasketCreated(id, 1, DateTime.UtcNow);
        Apply(created);
        _pendingEvents.Add(created);
    }

    public static Basket FromEvents(IEnumerable<IBasketEvent> events)
    {
        var basket = new Basket();
        foreach (var e in events)
            basket.Apply(e);
        return basket;
    }

    public static Basket Create(string id) => new(id);

    public void AddItem(string productId, int quantity, int sequenceNumber)
    {
        var e = new BasketItemAdded(productId, quantity, sequenceNumber, DateTime.UtcNow);
        Apply(e);
        _pendingEvents.Add(e);
    }

    public void AdjustItemQuantity(string productId, int quantityDelta, int sequenceNumber)
    {
        var item = _items.SingleOrDefault(i => i.ProductId == productId);
        if (item is null)
            throw new ApplicationException("Basket item not found");

        var newQuantity = item.Quantity + quantityDelta;

        IBasketEvent e = newQuantity <= 0
            ? new BasketItemRemoved(productId, sequenceNumber, DateTime.UtcNow)
            : new BasketItemQuantityAdjusted(productId, quantityDelta, sequenceNumber, DateTime.UtcNow);

        Apply(e);
        _pendingEvents.Add(e);
    }

    public void SetDiscountCode(string code, int sequenceNumber)
    {
        var e = new BasketDiscountCodeSet(code, sequenceNumber, DateTime.UtcNow);
        Apply(e);
        _pendingEvents.Add(e);
    }

    public void SetShippingCost(string countryCode, decimal cost, int sequenceNumber)
    {
        var e = new BasketShippingCostSet(countryCode, cost, sequenceNumber, DateTime.UtcNow);
        Apply(e);
        _pendingEvents.Add(e);
    }

    public void ClearPendingEvents() => _pendingEvents.Clear();

    private void Apply(IBasketEvent e)
    {
        switch (e)
        {
            case BasketCreated created: Apply(created); break;
            case BasketItemAdded added: Apply(added); break;
            case BasketItemQuantityAdjusted itemQuantityAdjusted: Apply(itemQuantityAdjusted); break;
            case BasketItemRemoved itemRemove: Apply(itemRemove); break;
            case BasketDiscountCodeSet discountCodeSet: Apply(discountCodeSet); break;
            case BasketShippingCostSet shipping: Apply(shipping); break;
        }
    }

    private void Apply(BasketCreated e) => Id = e.BasketId;

    private void Apply(BasketItemAdded e)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == e.ProductId);
        if (existing is not null)
            existing.Increase(e.Quantity);
        else
            _items.Add(new BasketItem(e.ProductId, e.Quantity));
    }

    private void Apply(BasketItemQuantityAdjusted e)
    {
        var item = _items.SingleOrDefault(i => i.ProductId == e.ProductId);
        if (item is null)
            throw new ApplicationException("Basket item not found");

        item.Increase(e.QuantityDelta);
    }

    private void Apply(BasketItemRemoved e)
    {
        var item = _items.SingleOrDefault(i => i.ProductId == e.ProductId);
        if (item is null)
            throw new ApplicationException("Basket item not found");

        _items.Remove(item);
    }

    private void Apply(BasketDiscountCodeSet e)
    {
        DiscountCode = e.Code;
    }

    private void Apply(BasketShippingCostSet e)
    {
        _shippingCosts[e.CountryCode] = e.Cost;
    }
}