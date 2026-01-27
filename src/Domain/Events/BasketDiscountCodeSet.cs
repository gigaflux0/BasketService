namespace Domain.Events;

public sealed record BasketDiscountCodeSet(
    string Code,
    int SequenceNumber,
    DateTime OccurredAt
    ) : IBasketEvent;