using Application.Abstractions;
using Application.Ports;
using FluentResults;

namespace Application.Basket.Commands.AddBasketItems;

public sealed class AddBasketItemsHandler : ICommandHandler<AddBasketItemsCommand>
{
    private readonly IBasketRepository _basketRepository;

    public AddBasketItemsHandler(IBasketRepository basketRepository)
    {
        _basketRepository = basketRepository;
    }

    public async Task<Result> Handle(AddBasketItemsCommand command, CancellationToken ct)
    {
        try
        {
            var streamId = $"basket-{command.BasketId}";
            var existingEvents = await _basketRepository.LoadEventsAsync(streamId, ct);
            var sequenceNumber = existingEvents.Count;

            var basket = existingEvents.Count == 0
                ? Domain.Basket.Create(command.BasketId)
                : Domain.Basket.FromEvents(existingEvents);

            sequenceNumber = sequenceNumber == 0 ? 1 : sequenceNumber;
            foreach (var item in command.Items)
                basket.AddItem(item.ProductId, item.Quantity, ++sequenceNumber);

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