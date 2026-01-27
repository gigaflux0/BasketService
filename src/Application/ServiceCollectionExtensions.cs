using Application.Abstractions;
using Application.Basket.Commands.AddBasketItems;
using Application.Basket.Commands.AdjustBasketItemQuantity;
using Application.Basket.Commands.SetDiscountCode;
using Application.Basket.Commands.SetShippingCost;
using Application.Basket.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection s) => s
        .AddScoped<ICommandHandler<AddBasketItemsCommand>, AddBasketItemsHandler>()
        .AddScoped<ICommandHandler<AdjustBasketItemQuantityCommand>, AdjustBasketItemQuantityHandler>()
        .AddScoped<ICommandHandler<SetDiscountCodeCommand>, SetDiscountCodeHandler>()
        .AddScoped<ICommandHandler<SetShippingCostCommand>, SetShippingCostHandler>()
        .AddScoped<IQueryHandler<GetBasketTotalsQuery, BasketTotalsResult>, GetBasketTotalsHandler>();
}