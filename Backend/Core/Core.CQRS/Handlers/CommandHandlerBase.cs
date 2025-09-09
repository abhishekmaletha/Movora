using MediatR;
using Microsoft.Extensions.Logging;
using Core.CQRS.Commands;
using Core.CQRS.Common;
using FluentValidation;

namespace Core.CQRS.Handlers;
public abstract class CommandBaseHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : CommandBase<TResponse> where TResponse : class
{
    abstract public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
