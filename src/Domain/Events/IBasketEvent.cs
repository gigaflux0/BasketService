namespace Domain.Events;

public interface IBasketEvent
{
    DateTime OccurredAt { get; }
    int SequenceNumber { get; }
}
