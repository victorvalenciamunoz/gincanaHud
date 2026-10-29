namespace GincanaHud.App.Services;

/// <summary>
/// Sends request bodies with Content-Length. <c>PostAsJsonAsync</c> streams them chunked, and Static Web Apps
/// answers "Backend call failure" (500) to chunked bodies for its managed functions (D22).
/// </summary>
public sealed class BufferedRequestContentHandler : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.Content is not null)
			await request.Content.LoadIntoBufferAsync(cancellationToken);

		return await base.SendAsync(request, cancellationToken);
	}
}
