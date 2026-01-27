using FluentResults;

namespace Application.Abstractions;

public interface ICommandHandler<in TCommand>
{
    Task<Result> Handle(TCommand command, CancellationToken ct);
}