using Application.Abstractions;
using Application.Ports;
using FluentResults;

namespace Application.Basket.Commands.SetShippingCost;

public sealed class SetShippingCostHandler : ICommandHandler<SetShippingCostCommand>
{
    private readonly IBasketRepository _basketRepository;

    public SetShippingCostHandler(IBasketRepository repository)
    {
        _basketRepository = repository;
    }

    public async Task<Result> Handle(SetShippingCostCommand command, CancellationToken ct)
    {
        try
        {
            var streamId = $"basket-{command.BasketId}";
            var existingEvents = await _basketRepository.LoadEventsAsync(streamId, ct);
            if (existingEvents.Count == 0)
            {
                return Result.Fail("Attempted to adjust an empty basket, add items first.");
            }

            var basket = Domain.Basket.FromEvents(existingEvents);

            basket.SetShippingCost(command.CountryCode, command.Cost, existingEvents.Count + 1);

            await _basketRepository.SaveEventsAsync(
                streamId,
                basket.PendingEvents.ToList(),
                expectedEventCountBeforeSaving: existingEvents.Count,
                ct);

            basket.ClearPendingEvents();

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError(ex));
        }
    }
}
