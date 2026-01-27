using Domain;
using Microsoft.Extensions.Hosting;
using ProjectionWorker.Models;
using ProjectionWorker.Ports;

namespace ProjectionWorker;

public class BasketProjectionWorker : BackgroundService
{
    private readonly IEventStreamSubscription _subscription;
    private readonly IEventStreamReader _eventReader;
    private readonly IBasketProjectionWriter _projectionWriter;

    public BasketProjectionWorker(
        IEventStreamSubscription subscription,
        IEventStreamReader eventReader,
        IBasketProjectionWriter projectionWriter)
    {
        _subscription = subscription;
        _eventReader = eventReader;
        _projectionWriter = projectionWriter;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _subscription.StartAsync(HandleEventAsync, stoppingToken);

    private async Task HandleEventAsync(StreamEventNotification notification, CancellationToken ct)
    {
        var events = await _eventReader.LoadEventsAsync(notification.StreamId, ct);
        var basket = Basket.FromEvents(events);

        var projection = new BasketProjection
        {
            Id = notification.StreamId,
            DiscountCode = basket.DiscountCode,
            Items = basket.Items
                .Select(i => new BasketItemProjection
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                })
                .ToList(),
            ShippingCosts = basket.ShippingCosts
        };

        await _projectionWriter.SaveAsync(projection, ct);
    }
}
