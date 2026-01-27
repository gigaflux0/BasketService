namespace Domain.Events;

public sealed record BasketCreated(string BasketId, int SequenceNumber, DateTime OccurredAt) : IBasketEvent;
