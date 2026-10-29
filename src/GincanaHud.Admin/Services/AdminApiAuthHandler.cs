using System.Net;
using GincanaHud.Shared;

namespace GincanaHud.Admin.Services;

/// <summary>
/// Sends the session's JWT to the Api in <see cref="ApiHeaders.PanelToken"/>: Static Web Apps does not pass
/// <c>Authorization</c> on to managed functions. A 401 means the token is no longer valid: the session is closed
/// and the router sends the user back to the login.
/// </summary>
public sealed class AdminApiAuthHandler(AdminSession session) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var token = await session.GetAccessTokenAsync();
		if (!string.IsNullOrWhiteSpace(token))
			request.Headers.Add(ApiHeaders.PanelToken, token);

		var response = await base.SendAsync(request, cancellationToken);
		if (response.StatusCode == HttpStatusCode.Unauthorized && token is not null)
			await session.SignOutAsync();

		return response;
	}
}
