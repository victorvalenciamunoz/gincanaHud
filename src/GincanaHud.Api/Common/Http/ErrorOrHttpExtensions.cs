using ErrorOr;

namespace GincanaHud.Api.Common.Http;

public static class ErrorOrHttpExtensions
{
	public static IResult ToHttpResult<T>(this ErrorOr<T> result, Func<T, IResult>? onValue = null)
	{
		if (!result.IsError)
			return onValue?.Invoke(result.Value) ?? Results.Ok(result.Value);

		var error = result.FirstError;
		return error.Type switch
		{
			ErrorType.Validation => Results.BadRequest(error.Description),
			ErrorType.NotFound => Results.NotFound(error.Description),
			ErrorType.Conflict => Results.Conflict(error.Description),
			ErrorType.Unauthorized => Results.Unauthorized(),
			ErrorType.Forbidden => Results.Json(new { error = error.Description }, statusCode: StatusCodes.Status403Forbidden),
			_ => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status500InternalServerError)
		};
	}
}
