using MediatR;
using Microsoft.Extensions.Logging;
using Core.CQRS.Queries;
using Core.CQRS.Common;
using FluentValidation;


namespace Core.CQRS.Handlers;
    public abstract class QueryBaseHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : QueryBase<TResponse> where TResponse : class
    {
        public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }