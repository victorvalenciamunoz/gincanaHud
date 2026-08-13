namespace GincanaHud.Api.Common.Messaging;

public interface IRequest<TResponse>;

public interface IRequestHandler<in TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface ISender
{
	Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
