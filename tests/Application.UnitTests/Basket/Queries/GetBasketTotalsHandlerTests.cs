using Application.Basket.Queries;
using Application.Ports;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.UnitTests.Basket.Queries;

[TestFixture]
public class GetBasketTotalsHandlerTests
{
    private Fixture _fixture = null!;
    private IBasketReadModelRepository _readModel = null!;
    private IPriceApi _priceApi = null!;
    private IDiscountCodesApi _discountApi = null!;
    private GetBasketTotalsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _readModel = Substitute.For<IBasketReadModelRepository>();
        _priceApi = Substitute.For<IPriceApi>();
        _discountApi = Substitute.For<IDiscountCodesApi>();
        _handler = new GetBasketTotalsHandler(_readModel, _priceApi, _discountApi);
    }

    [Test]
    public async Task Handle_WhenBasketNotFound_ReturnsFailure()
    {
        var query = _fixture.Create<GetBasketTotalsQuery>();
        var streamId = $"basket-{query.BasketId}";

        _readModel.GetAsync(streamId, Arg.Any<CancellationToken>())
            .Returns((BasketReadModel?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task Handle_WhenBasketHasItemsAndNoDiscountCode_ComputesTotals()
    {
        var query = _fixture.Create<GetBasketTotalsQuery>();
        var streamId = $"basket-{query.BasketId}";

        var basket = new BasketReadModel(
            query.BasketId,
            null,
            new List<BasketReadModelItem>
            {
                new BasketReadModelItem("prod-1", 2),
                new BasketReadModelItem("prod-2", 1)
            },
            new Dictionary<string, decimal>
            {
                { query.Country, 10m }
            }
        );

        _readModel.GetAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(basket);

        _priceApi.GetPriceAsync("prod-1", Arg.Any<CancellationToken>())
            .Returns(new PriceResult("prod-1", 100m, 80m));

        _priceApi.GetPriceAsync("prod-2", Arg.Any<CancellationToken>())
            .Returns(new PriceResult("prod-2", 50m, 50m));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var totals = result.Value;

        totals.SubtotalBeforeDiscounts.Should().Be(250m);
        totals.ItemDiscounts.Should().Be(40m);
        totals.DiscountCodeAmount.Should().Be(0m);
        totals.SubtotalAfterDiscounts.Should().Be(210m);
        totals.VatOnItems.Should().Be(42m);
        totals.ShippingCost.Should().Be(10m);
        totals.ShippingVat.Should().Be(2m);
        totals.TotalWithoutVat.Should().Be(220m);
        totals.TotalWithVat.Should().Be(264m);
        totals.TotalSavings.Should().Be(40m);
    }

    [Test]
    public async Task Handle_WhenBasketHasPercentageDiscount_AppliesDiscount()
    {
        var query = _fixture.Create<GetBasketTotalsQuery>();
        var streamId = $"basket-{query.BasketId}";

        var basket = new BasketReadModel(
            query.BasketId,
            "SAVE10",
            new List<BasketReadModelItem>
            {
                new BasketReadModelItem("prod-1", 2)
            },
            new Dictionary<string, decimal>
            {
                { query.Country, 0m }
            }
        );

        _readModel.GetAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(basket);

        _priceApi.GetPriceAsync("prod-1", Arg.Any<CancellationToken>())
            .Returns(new PriceResult("prod-1", 100m, 100m));

        _discountApi.GetDiscountAsync("SAVE10", Arg.Any<CancellationToken>())
            .Returns(new DiscountCodeResult("SAVE10", "percentage", 10m));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var totals = result.Value;

        totals.DiscountCodeAmount.Should().Be(20m);
        totals.TotalSavings.Should().Be(20m);
    }

    [Test]
    public async Task Handle_WhenBasketHasFixedDiscount_AppliesDiscount()
    {
        var query = _fixture.Create<GetBasketTotalsQuery>();
        var streamId = $"basket-{query.BasketId}";

        var basket = new BasketReadModel(
            query.BasketId,
            "SAVE5",
            new List<BasketReadModelItem>
            {
                new BasketReadModelItem("prod-1", 1)
            },
            new Dictionary<string, decimal>
            {
                { query.Country, 0m }
            }
        );

        _readModel.GetAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(basket);

        _priceApi.GetPriceAsync("prod-1", Arg.Any<CancellationToken>())
            .Returns(new PriceResult("prod-1", 50m, 50m));

        _discountApi.GetDiscountAsync("SAVE5", Arg.Any<CancellationToken>())
            .Returns(new DiscountCodeResult("SAVE5", "fixed", 5m));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var totals = result.Value;

        totals.DiscountCodeAmount.Should().Be(5m);
        totals.TotalSavings.Should().Be(5m);
    }

    [Test]
    public async Task Handle_WhenAnyDependencyThrows_ReturnsFailure()
    {
        var query = _fixture.Create<GetBasketTotalsQuery>();
        var streamId = $"basket-{query.BasketId}";

        _readModel.GetAsync(streamId, Arg.Any<CancellationToken>())
            .Throws(new Exception("DB unavailable"));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ExceptionalError>();
    }
}