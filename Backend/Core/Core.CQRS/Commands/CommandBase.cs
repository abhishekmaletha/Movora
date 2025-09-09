using MediatR;
namespace Core.CQRS.Commands;

public abstract class CommandBase<TResult> : ICommand, IRequest<TResult> where TResult : class
 {
 }