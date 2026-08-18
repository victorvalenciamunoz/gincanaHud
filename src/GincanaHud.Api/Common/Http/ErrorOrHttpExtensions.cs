using ErrorOr;

namespace GincanaHud.Api.Common.Http;

public static class ErrorOrHttpExtensions
{
	public static IResult ToHttpResult<T>(this ErrorOr<T> result, Func<T, IResult>? onValue = null)
	{
		if (!result.IsError)
			return onValue?.Invoke(result.Value) ?? Results.Ok(result.Value);

		return new DomainErrorResult(result.FirstError);
	}

	private static int StatusCodeFor(ErrorType type) => type switch
	{
		ErrorType.Validation => StatusCodes.Status400BadRequest,
		ErrorType.NotFound => StatusCodes.Status404NotFound,
		ErrorType.Conflict => StatusCodes.Status409Conflict,
		ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
		ErrorType.Forbidden => StatusCodes.Status403Forbidden,
		_ => StatusCodes.Status500InternalServerError
	};

	private sealed class DomainErrorResult(Error error) : IResult
	{
		public Task ExecuteAsync(HttpContext http)
		{
			var status = StatusCodeFor(error.Type);
			var logger = http.RequestServices
				.GetService<ILoggerFactory>()
				?.CreateLogger("GincanaHud.Api.Errors");

			logger?.LogWarning(
				"{Method} {Path} → {Status} [{Code}] {Detail}",
				http.Request.Method,
				http.Request.Path.Value,
				status,
				error.Code,
				error.Description);

			if (error.Type is ErrorType.Unauthorized)
			{
				http.Response.StatusCode = status;
				return Task.CompletedTask;
			}

			return Results.Problem(
				detail: error.Description,
				statusCode: status,
				title: error.Code,
				instance: http.Request.Path.Value,
				extensions: new Dictionary<string, object?>
				{
					["code"] = error.Code
				}).ExecuteAsync(http);
		}
	}
}
