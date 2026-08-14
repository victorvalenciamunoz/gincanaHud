using System.Net.Http.Headers;

namespace GincanaHud.Admin.Services;

/// <summary>
/// Forwards the JWT from the Admin cookie claim to the Api as Bearer.
/// </summary>
public sealed class AdminApiAuthHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
	public const string AccessTokenClaimType = "access_token";

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var token = httpContextAccessor.HttpContext?.User?.FindFirst(AccessTokenClaimType)?.Value;
		if (!string.IsNullOrWhiteSpace(token))
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		return base.SendAsync(request, cancellationToken);
	}
}
