namespace GincanaHud.Api.Common.Messaging;

public sealed class Sender(IServiceProvider services) : ISender
{
	public async Task<TResponse> Send<TResponse>(
		IRequest<TResponse> request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var handlerType = typeof(IRequestHandler<,>)
			.MakeGenericType(request.GetType(), typeof(TResponse));
		dynamic handler = services.GetRequiredService(handlerType);
		return await handler.Handle((dynamic)request, cancellationToken);
	}
}
