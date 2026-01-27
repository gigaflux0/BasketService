using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Application.Ports;
using BasketService.Api.Endpoints.Basket.Requests;
using BasketService.Api.Endpoints.Basket.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace BasketService.Api.IntegrationTests;

[TestFixture]
public class BasketEndpointsTests
{
    private HttpClient _client = null!;
    private ApiFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ApiFactory(CosmosDbFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void Cleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    [TestCaseSource(nameof(AddItemsCases))]
    public async Task PostBasketItems_WithValidItems_UpdatesReadModel(
        PostBasketItemsRequest.Item[] items,
        object[] expectedItems)
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString("N");
        var payload = new PostBasketItemsRequest(basketId, items);

        // Act
        var response = await _client.PostAsJsonAsync("/basket/items", payload);

        // Assert - write side
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Assert - read side
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBasketReadModelRepository>();

        var basket = await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b => b is { Items.Count: > 0 }
        );

        basket.Should().NotBeNull();
        basket.Items.Should().BeEquivalentTo(expectedItems);
    }

    [Test]
    [TestCaseSource(nameof(AdjustItemCases))]
    public async Task PatchBasketItem_AdjustsQuantity_UpdatesReadModel(
        PostBasketItemsRequest.Item[] initialItems,
        string itemId,
        int delta,
        object[] expectedItems)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBasketReadModelRepository>();
        var basketId = Guid.NewGuid().ToString("N");

        // Seed initial items
        var seedPayload = new PostBasketItemsRequest(basketId, initialItems);
        var seedResponse = await _client.PostAsJsonAsync("/basket/items", seedPayload);
        seedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b => b is { Items.Count: > 0 }
        );

        // Act: apply the delta
        var patchPayload = new PatchBasketItemsByIdRequest(basketId, delta);
        var patchResponse = await _client.PatchAsJsonAsync($"/basket/items/{itemId}", patchPayload);

        // Assert - write side
        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - read side
        var expectedQuantity = expectedItems.Length == 0 ? 0 : seedPayload.Items[0].Quantity + patchPayload.QuantityDelta;
        var expectedItemCount = expectedQuantity <= 0 ? 0 : 1;
        var basket = await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b =>
                b is not null && b.Items.Count == expectedItemCount &&
                (expectedItemCount == 0 || b.Items[0].Quantity == expectedQuantity)
        );

        basket.Should().NotBeNull();
        basket.Items.Should().BeEquivalentTo(expectedItems);
    }

    /// <summary>
    ///     This test is based on the hard coded fake prices api, which makes every base price 10 and discount 8.
    /// Unless the products name is specifically notDiscounted which makes the price just 10.
    /// </summary>
    [Test]
    [TestCaseSource(nameof(BasketTotalsCases))]
    public async Task GetBasketTotals_ReturnsCorrectTotals(
        PostBasketItemsRequest.Item[] items,
        string countryCode,
        decimal expectedTotalExVat,
        decimal expectedTotalIncVat)
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString("N");

        // Seed items
        var payload = new PostBasketItemsRequest(basketId, items);
        var response = await _client.PostAsJsonAsync("/basket/items", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Wait for read model
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBasketReadModelRepository>();

        await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b => b is { Items.Count: > 0 }
        );

        // Act: call totals endpoint
        var totalsResponse = await _client.GetAsync($"/basket/totals?basketId={basketId}&countryCode={countryCode}");
        totalsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var totals = await totalsResponse.Content.ReadFromJsonAsync<GetBasketTotalsByIdResponse>();

        // Assert
        totals.Should().NotBeNull();
        totals.TotalWithoutVat.Should().Be(expectedTotalExVat);
        totals.TotalWithVat.Should().Be(expectedTotalIncVat);
    }

    [Test]
    [TestCaseSource(nameof(ShippingCostCases))]
    public async Task PutBasketShipping_SetsShippingCost_ForCountry(
        Dictionary<string, decimal> countryCosts)
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString("N");

        // Seed a basket with at least one item
        var seedPayload = new PostBasketItemsRequest(
            basketId,
            [new PostBasketItemsRequest.Item("123", 1)]
        );

        // Seed write
        var seedResponse = await _client.PostAsJsonAsync("/basket/items", seedPayload);
        seedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBasketReadModelRepository>();

        // Wait for read model
        await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b => b is { Items.Count: > 0 }
        );

        // Act + Assert for each country
        foreach (var (countryCode, shippingCost) in countryCosts)
        {
            // Set shipping cost
            var putPayload = new PutBasketShippingByCountryCodeRequest(basketId, shippingCost);
            var putResponse = await _client.PutAsJsonAsync($"/basket/shipping/{countryCode}", putPayload);
            putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Wait for totals to reflect the shipping cost
            var totals = await Eventually.WaitFor(
                action: async () =>
                {
                    var response = await _client.GetAsync($"/basket/totals?basketId={basketId}&countryCode={countryCode}");
                    if (response.StatusCode != HttpStatusCode.OK)
                        return null;

                    return await response.Content.ReadFromJsonAsync<GetBasketTotalsByIdResponse>();
                },
                condition: t =>
                    t is not null &&
                    t.ShippingCost == shippingCost
            );

            totals.Should().NotBeNull();
            totals!.ShippingCost.Should().Be(shippingCost);
        }
    }

    [Test]
    [TestCaseSource(nameof(DiscountCodeCases))]
    public async Task PutBasketDiscountCode_AppliesDiscountToNonDiscountedItemsOnly(
        PostBasketItemsRequest.Item[] items,
        string discountCode,
        decimal expectedTotalWithoutVat,
        decimal expectedTotalWithVat)
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString("N");

        // Seed items
        var seedPayload = new PostBasketItemsRequest(basketId, items);
        var seedResponse = await _client.PostAsJsonAsync("/basket/items", seedPayload);
        seedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBasketReadModelRepository>();

        // Wait for items to appear in read model
        await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b => b is { Items.Count: > 0 }
        );

        // Act: apply discount code
        var putPayload = new PutBasketDiscountCodeRequest(basketId, discountCode);
        var putResponse = await _client.PutAsJsonAsync("/basket/discount-code", putPayload);

        // Assert - write side
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Wait for discount code to appear in read model
        var basketWithDiscount = await Eventually.WaitFor(
            action: () => repo.GetAsync($"basket-{basketId}", CancellationToken.None),
            condition: b => b is not null && b.DiscountCode == discountCode
        );

        basketWithDiscount.Should().NotBeNull();
        basketWithDiscount!.DiscountCode.Should().Be(discountCode);

        // Now that the read model is consistent, call totals normally
        var totalsResponse = await _client.GetAsync($"/basket/totals?basketId={basketId}&countryCode=UK");
        totalsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var totals = await totalsResponse.Content.ReadFromJsonAsync<GetBasketTotalsByIdResponse>();

        totals.Should().NotBeNull();
        totals!.TotalWithoutVat.Should().Be(expectedTotalWithoutVat);
        totals.TotalWithVat.Should().Be(expectedTotalWithVat);
    }

    public static IEnumerable<TestCaseData> AddItemsCases()
    {
        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 1)
            },
            new[]
            {
                new { ProductId = "123", Quantity = 1 }
            }).SetName("Add an item to the basket");

        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 1),
                new PostBasketItemsRequest.Item("456", 2)
            },
            new[]
            {
                new { ProductId = "123", Quantity = 1 },
                new { ProductId = "456", Quantity = 2 }
            }).SetName("Add multiple items to the basket");

        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 1),
                new PostBasketItemsRequest.Item("123", 2)
            },
            new[]
            {
                new { ProductId = "123", Quantity = 3 }
            }).SetName("Add multiple of the same item to the basket");
    }

    public static IEnumerable<TestCaseData> AdjustItemCases()
    {
        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 5)
            },
            "123",
            -2,
            new[]
            {
                new { ProductId = "123", Quantity = 3 }
            }
        ).SetName("Decrease quantity but keep item");

        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 2)
            },
            "123",
            -2,
            Array.Empty<object>()
        ).SetName("Decrease quantity to zero removes item");

        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 2)
            },
            "123",
            -5,
            Array.Empty<object>()
        ).SetName("Decrease quantity below zero removes item");
    }

    public static IEnumerable<TestCaseData> BasketTotalsCases()
    {
        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 2)
            },
            "UK",
            16m,
            19.2m
        ).SetName("Totals for single item basket");

        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("123", 1),
                new PostBasketItemsRequest.Item("456", 3)
            },
            "UK",
            32m,
            38.4m
        ).SetName("Totals for multi‑item basket");
    }

    public static IEnumerable<TestCaseData> ShippingCostCases()
    {
        yield return new TestCaseData(
            new Dictionary<string, decimal>
            {
                ["GB"] = 5m,
                ["DE"] = 7.5m,
                ["FR"] = 12m
            }
        ).SetName("Set shipping cost for multiple countries");
    }

    public static IEnumerable<TestCaseData> DiscountCodeCases()
    {
        yield return new TestCaseData(
            new[]
            {
                new PostBasketItemsRequest.Item("A", 1),
                new PostBasketItemsRequest.Item("notDiscounted", 1)
            },
            "HALF",
            13m,
            15.6m
        ).SetName("Apply HALF discount code excluding discounted items");
    }
}