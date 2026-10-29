using System.Text.Json;

namespace GincanaHud.Api.Common.Http;

public static class HttpRequestBodyExtensions
{
	/// <summary>
	/// Reads the JSON body with the app's HTTP JSON options and hands it to <paramref name="handle"/>.
	/// A missing or malformed body answers 400, as Minimal API did.
	/// </summary>
	public static async Task<IResult> WithJsonBodyAsync<T>(
		this HttpRequest request, Func<T, Task<IResult>> handle, CancellationToken ct)
	{
		T? body;
		try
		{
			body = await request.ReadFromJsonAsync<T>(ct);
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			return Results.BadRequest();
		}

		return body is null ? Results.BadRequest() : await handle(body);
	}
}
