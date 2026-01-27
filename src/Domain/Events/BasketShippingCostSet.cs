namespace Domain.Events;

public sealed record BasketShippingCostSet(
    string CountryCode,
    decimal Cost,
    int SequenceNumber,
    DateTime OccurredAt
    ) : IBasketEvent;