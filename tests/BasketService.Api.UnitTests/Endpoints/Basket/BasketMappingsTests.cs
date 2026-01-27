using BasketService.Api.Endpoints.Basket;
using BasketService.Api.Endpoints.Basket.Requests;
using Application.Basket.Queries;
using FluentAssertions;

namespace BasketService.Api.UnitTests.Endpoints.Basket;

[TestFixture]
public class BasketMappingsTests
{
    [Test]
    public void PostBasketItemsRequest_MapsToAddBasketItemsCommand()
    {
        var request = new PostBasketItemsRequest(
            BasketId: "basket-1",
            Items:
            [
                new("prod-1", 2),
                new("prod-2", 3)
            ]
        );

        var command = request.ToCommand();

        command.BasketId.Should().Be("basket-1");
        command.Items.Should().HaveCount(2);
        command.Items.Should().Contain(i => i.ProductId == "prod-1" && i.Quantity == 2);
        command.Items.Should().Contain(i => i.ProductId == "prod-2" && i.Quantity == 3);
    }

    [Test]
    public void PatchBasketItemsByIdRequest_MapsToAdjustBasketItemQuantityCommand()
    {
        var request = new PatchBasketItemsByIdRequest(
            BasketId: "basket-1",
            QuantityDelta: 5
        );

        var command = request.ToCommand("prod-1");

        command.BasketId.Should().Be("basket-1");
        command.ProductId.Should().Be("prod-1");
        command.QuantityDelta.Should().Be(5);
    }

    [Test]
    public void PutBasketDiscountCodeRequest_MapsToSetDiscountCodeCommand()
    {
        var request = new PutBasketDiscountCodeRequest(
            BasketId: "basket-1",
            DiscountCode: "SAVE10"
        );

        var command = request.ToCommand();

        command.BasketId.Should().Be("basket-1");
        command.Code.Should().Be("SAVE10");
    }

    [Test]
    public void PutBasketShippingByCountryCodeRequest_MapsToSetShippingCostCommand()
    {
        var request = new PutBasketShippingByCountryCodeRequest(
            BasketId: "basket-1",
            Cost: 4.99m
        );

        var command = request.ToCommand("UK");

        command.BasketId.Should().Be("basket-1");
        command.CountryCode.Should().Be("UK");
        command.Cost.Should().Be(4.99m);
    }

    [Test]
    public void BasketTotalsResult_MapsToResponse()
    {
        var result = new BasketTotalsResult(
            SubtotalBeforeDiscounts: 100,
            ItemDiscounts: 10,
            DiscountCodeAmount: 5,
            SubtotalAfterDiscounts: 90,
            VatOnItems: 20,
            ShippingCost: 4.99m,
            ShippingVat: 1.00m,
            TotalWithoutVat: 110,
            TotalWithVat: 111,
            TotalSavings: 15
        );

        var response = result.ToResponse();

        response.SubtotalBeforeDiscounts.Should().Be(100);
        response.ItemDiscounts.Should().Be(10);
        response.DiscountCodeAmount.Should().Be(5);
        response.SubtotalAfterDiscounts.Should().Be(90);
        response.VatOnItems.Should().Be(20);
        response.ShippingCost.Should().Be(4.99m);
        response.ShippingVat.Should().Be(1.00m);
        response.TotalWithoutVat.Should().Be(110);
        response.TotalWithVat.Should().Be(111);
        response.TotalSavings.Should().Be(15);
    }
}