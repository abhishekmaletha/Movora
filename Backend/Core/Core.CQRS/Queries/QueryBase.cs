using Core.CQRS.Queries;
using MediatR;

namespace Core.CQRS.Queries;

public abstract class QueryBase<TResult> : IQuery, IRequest<TResult> where TResult : class
{
}