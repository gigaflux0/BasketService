using AutoFixture;
using Domain.Events;
using FluentAssertions;

namespace Domain.UnitTests;

[TestFixture]
public class BasketTests
{
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
    }

    [Test]
    public void CreatingABasket_RaisesBasketCreatedEvent()
    {
        var id = _fixture.Create<string>();

        var basket = Basket.Create(id);

        basket.PendingEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BasketCreated>()
            .Which.BasketId.Should().Be(id);
    }

    [Test]
    public void AddingANewItem_AddsItToTheBasket_AndRaisesEvent()
    {
        var basket = Basket.Create("basket-1");

        basket.AddItem("prod-1", 2, sequenceNumber: 2);

        basket.Items.Should().ContainSingle(i =>
            i.ProductId == "prod-1" &&
            i.Quantity == 2);

        basket.PendingEvents.Last().Should().BeOfType<BasketItemAdded>()
            .Which.ProductId.Should().Be("prod-1");
    }

    [Test]
    public void AddingAnExistingItem_IncreasesQuantity()
    {
        var basket = Basket.Create("basket-1");

        basket.AddItem("prod-1", 2, 2);
        basket.AddItem("prod-1", 3, 3);

        basket.Items.Single().Quantity.Should().Be(5);
    }

    [Test]
    public void AdjustingQuantity_ForExistingItem_AdjustsQuantity()
    {
        var basket = Basket.Create("basket-1");
        basket.AddItem("prod-1", 2, 2);

        basket.AdjustItemQuantity("prod-1", quantityDelta: 3, sequenceNumber: 3);

        basket.Items.Single().Quantity.Should().Be(5);

        basket.PendingEvents.Last().Should().BeOfType<BasketItemQuantityAdjusted>();
    }

    [Test]
    public void AdjustingQuantity_ToZeroOrBelow_RemovesItem()
    {
        var basket = Basket.Create("basket-1");
        basket.AddItem("prod-1", 2, 2);

        basket.AdjustItemQuantity("prod-1", quantityDelta: -2, sequenceNumber: 3);

        basket.Items.Should().BeEmpty();
        basket.PendingEvents.Last().Should().BeOfType<BasketItemRemoved>();
    }

    [Test]
    public void AdjustingQuantity_ForMissingItem_Throws()
    {
        var basket = Basket.Create("basket-1");

        var actual = () => basket.AdjustItemQuantity("prod-1", 1, 2);

        actual.Should().Throw<ApplicationException>()
            .WithMessage("Basket item not found");
    }

    [Test]
    public void SettingDiscountCode_UpdatesBasket_AndRaisesEvent()
    {
        var basket = Basket.Create("basket-1");

        basket.SetDiscountCode("SAVE10", 2);

        basket.DiscountCode.Should().Be("SAVE10");
        basket.PendingEvents.Last().Should().BeOfType<BasketDiscountCodeSet>();
    }

    [Test]
    public void SettingShippingCost_StoresCostPerCountry_AndRaisesEvent()
    {
        var basket = Basket.Create("basket-1");

        basket.SetShippingCost("UK", 4.99m, 2);

        basket.ShippingCosts["UK"].Should().Be(4.99m);
        basket.PendingEvents.Last().Should().BeOfType<BasketShippingCostSet>();
    }

    [Test]
    public void FromEvents_RebuildsBasketState()
    {
        const string id = "basket-1";

        var events = new IBasketEvent[]
        {
            new BasketCreated(id, 1, DateTime.UtcNow),
            new BasketItemAdded("prod-1", 2, 2, DateTime.UtcNow),
            new BasketDiscountCodeSet("SAVE10", 3, DateTime.UtcNow),
            new BasketShippingCostSet("UK", 4.99m, 4, DateTime.UtcNow)
        };

        var basket = Basket.FromEvents(events);

        basket.Items.Should().ContainSingle(i =>
            i.ProductId == "prod-1" &&
            i.Quantity == 2);

        basket.DiscountCode.Should().Be("SAVE10");
        basket.ShippingCosts["UK"].Should().Be(4.99m);
    }

    [Test]
    public void ClearingPendingEvents_RemovesAllPendingEvents()
    {
        var basket = Basket.Create("basket-1");
        basket.AddItem("prod-1", 2, 2);

        basket.ClearPendingEvents();

        basket.PendingEvents.Should().BeEmpty();
    }
}
