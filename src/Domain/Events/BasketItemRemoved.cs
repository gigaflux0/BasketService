namespace Domain.Events;

public sealed record BasketItemRemoved(
    string ProductId,
    int SequenceNumber,
    DateTime OccurredAt) : IBasketEvent;