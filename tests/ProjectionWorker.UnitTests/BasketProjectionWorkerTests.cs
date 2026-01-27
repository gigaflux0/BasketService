using System.Reflection;
using AutoFixture;
using Domain.Events;
using FluentAssertions;
using NSubstitute;
using ProjectionWorker.Models;
using ProjectionWorker.Ports;

namespace ProjectionWorker.UnitTests;

[TestFixture]
public class BasketProjectionWorkerTests
{
    private Fixture _fixture;
    private IEventStreamSubscription _subscription;
    private IEventStreamReader _eventReader;
    private IBasketProjectionWriter _projectionWriter;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _subscription = Substitute.For<IEventStreamSubscription>();
        _eventReader = Substitute.For<IEventStreamReader>();
        _projectionWriter = Substitute.For<IBasketProjectionWriter>();
    }

    [Test]
    public async Task WhenWorkerStarts_SubscribesHandlerToEventStream()
    {
        var worker = new BasketProjectionWorker(_subscription, _eventReader, _projectionWriter);
        await worker.StartAsync(CancellationToken.None);

        await _subscription
            .Received(1)
            .StartAsync(
                Arg.Any<Func<StreamEventNotification, CancellationToken, Task>>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandlerBuildsAProjection_AndSavesIt()
    {
        // Arrange
        var streamId = _fixture.Create<string>();

        var events = new IBasketEvent[]
        {
            new BasketCreated(streamId, 1, DateTime.UtcNow),
            new BasketItemAdded("prod-1", 2, 2, DateTime.UtcNow),
            new BasketDiscountCodeSet("SAVE10", 3, DateTime.UtcNow),
            new BasketShippingCostSet("UK", 4.99m, 4, DateTime.UtcNow)
        };

        _eventReader
            .LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(events);

        var notification = new StreamEventNotification(streamId);

        // Act
        // We need to awkwardly invoke a private method for this test
        var handleMethod = typeof(BasketProjectionWorker)
            .GetMethod("HandleEventAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        handleMethod.Should().NotBeNull();

        var worker = new BasketProjectionWorker(_subscription, _eventReader, _projectionWriter);

        var result = handleMethod.Invoke(worker, [notification, CancellationToken.None]);
        result.Should().NotBeNull("HandleEventAsync must return a Task");

        await (Task)result;

        // Assert
        await _projectionWriter
            .Received(1)
            .SaveAsync(
                Arg.Is<BasketProjection>(p =>
                    p.Id == streamId &&
                    p.DiscountCode == "SAVE10" &&
                    p.Items.Count == 1 &&
                    p.Items.Single().ProductId == "prod-1" &&
                    p.Items.Single().Quantity == 2 &&
                    p.ShippingCosts["UK"] == 4.99m
                ),
                Arg.Any<CancellationToken>());
    }
}