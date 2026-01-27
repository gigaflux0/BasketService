namespace ProjectionWorker.Ports;

public interface IEventStreamSubscription
{
    Task StartAsync(Func<StreamEventNotification, CancellationToken, Task> onEvent, CancellationToken ct);
}

public sealed record StreamEventNotification(string StreamId);