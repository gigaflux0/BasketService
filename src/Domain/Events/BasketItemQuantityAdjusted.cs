namespace Domain.Events;

public sealed record BasketItemQuantityAdjusted(
    string ProductId,
    int QuantityDelta,
    int SequenceNumber,
    DateTime OccurredAt) : IBasketEvent;