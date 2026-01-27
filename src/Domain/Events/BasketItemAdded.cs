namespace Domain.Events;

public sealed record BasketItemAdded(
    string ProductId,
    int Quantity,
    int SequenceNumber,
    DateTime OccurredAt) : IBasketEvent;