using Application.Basket.Commands.SetDiscountCode;
using Application.Ports;
using AutoFixture;
using Domain.Events;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.UnitTests.Basket.Commands.SetDiscountCode;

[TestFixture]
public class SetDiscountCodeHandlerTests
{
    private Fixture _fixture = null!;
    private IBasketRepository _repository = null!;
    private SetDiscountCodeHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _repository = Substitute.For<IBasketRepository>();
        _handler = new SetDiscountCodeHandler(_repository);
    }

    [Test]
    public async Task Handle_WhenBasketDoesNotExist_ReturnsFailure()
    {
        var command = _fixture.Create<SetDiscountCodeCommand>();
        var streamId = $"basket-{command.BasketId}";

        _repository.LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(new List<IBasketEvent>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task Handle_WhenBasketExists_SetsDiscountCodeAndSavesEvents()
    {
        var command = _fixture.Create<SetDiscountCodeCommand>();
        var streamId = $"basket-{command.BasketId}";

        var existingEvents = new List<IBasketEvent>
        {
            new BasketCreated(command.BasketId, _fixture.Create<int>(), _fixture.Create<DateTime>())
        };

        _repository.LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Returns(existingEvents);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _repository.Received(1).SaveEventsAsync(
            streamId,
            Arg.Is<IReadOnlyList<IBasketEvent>>(events => events.Count == 1),
            expectedEventCountBeforeSaving: existingEvents.Count,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRepositoryThrows_ReturnsFailure()
    {
        var command = _fixture.Create<SetDiscountCodeCommand>();
        var streamId = $"basket-{command.BasketId}";

        _repository.LoadEventsAsync(streamId, Arg.Any<CancellationToken>())
            .Throws(new Exception("DB unavailable"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ExceptionalError>();
    }
}