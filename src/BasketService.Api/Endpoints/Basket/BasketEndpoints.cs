using Application.Abstractions;
using Application.Basket.Commands.AddBasketItems;
using Application.Basket.Commands.AdjustBasketItemQuantity;
using Application.Basket.Commands.SetDiscountCode;
using Application.Basket.Commands.SetShippingCost;
using Application.Basket.Queries;
using BasketService.Api.Endpoints.Basket.Requests;

namespace BasketService.Api.Endpoints.Basket;

public static class BasketEndpoints
{
    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/basket/items", async (
            PostBasketItemsRequest request,
            ICommandHandler<AddBasketItemsCommand> handler,
            CancellationToken ct) =>
        {
            var command = request.ToCommand();
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Accepted()
                : Results.BadRequest(result.Errors);
        });

        endpoints.MapPatch("/basket/items/{itemId}", async (
            string itemId,
            PatchBasketItemsByIdRequest request,
            ICommandHandler<AdjustBasketItemQuantityCommand> handler,
            CancellationToken ct) =>
        {
            var command = request.ToCommand(itemId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(result.Errors);
        });

        endpoints.MapPut("/basket/discount-code", async (
            PutBasketDiscountCodeRequest request,
            ICommandHandler<SetDiscountCodeCommand> handler,
            CancellationToken ct) =>
        {
            var command = request.ToCommand();
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(result.Errors);
        });

        endpoints.MapPut("/basket/shipping/{countryCode}", async (
            string countryCode,
            PutBasketShippingByCountryCodeRequest request,
            ICommandHandler<SetShippingCostCommand> handler,
            CancellationToken ct) =>
        {
            var command = request.ToCommand(countryCode);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(result.Errors);
        });

        endpoints.MapGet("/basket/totals", async (
            string basketId,
            string countryCode,
            IQueryHandler<GetBasketTotalsQuery, BasketTotalsResult> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new GetBasketTotalsQuery(basketId, countryCode),
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value.ToResponse())
                : Results.BadRequest(result.Errors);
        });

        return endpoints;
    }
}