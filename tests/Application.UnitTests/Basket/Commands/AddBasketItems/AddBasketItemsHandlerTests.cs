using Application.Basket.Commands.AddBasketItems;
using Application.Ports;
using AutoFixture;
using Domain.Events;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.UnitTests.Basket.Commands.AddBasketItems;

[TestFixture]
public class AddBasketItemsHandlerTests
{
    private Fixture _fixture = null!;
    private IBasketRepository _repository = null!;
    private AddBasketItemsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _repository = Substitute.For<IBasketRepository>();
        _handler = new AddBasketItemsHandler(_repository);
    }

    [Test]
    public async Task Handle_WhenNoExistingEvents_CreatesNewBasketAndSavesEvents()
    {
        // Arrange
        var command = _fixture.Create<AddBasketItemsCommand>();
        var streamId = $"basket-{command.BasketId}";

        _repository.LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(new List<IBasketEvent>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repository
            .Received(1)
            .SaveEventsAsync(
                streamId,
                Arg.Is<IReadOnlyList<IBasketEvent>>(events => events.Count == command.Items.Count + 1),
                0,
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExistingEvents_LoadsBasketAndAppendsNewEvents()
    {
        // Arrange
        var command = _fixture.Create<AddBasketItemsCommand>();
        var streamId = $"basket-{command.BasketId}";

        var existingEvents = new List<IBasketEvent>
        {
            new BasketCreated(command.BasketId, _fixture.Create<int>(), _fixture.Create<DateTime>()),
            new BasketItemAdded(command.BasketId, 1, 1, _fixture.Create<DateTime>())
        };

        _repository.LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(existingEvents);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repository.Received(1).SaveEventsAsync(
            streamId,
            Arg.Is<List<IBasketEvent>>(events =>
                events.Count == command.Items.Count // appended events only
            ),
            expectedEventCountBeforeSaving: existingEvents.Count,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRepositoryThrows_ReturnsFailure()
    {
        // Arrange
        var command = _fixture.Create<AddBasketItemsCommand>();
        var streamId = $"basket-{command.BasketId}";

        _repository.LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Throws(new Exception("DB unavailable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Should().BeOfType<ExceptionalError>();
    }
}
