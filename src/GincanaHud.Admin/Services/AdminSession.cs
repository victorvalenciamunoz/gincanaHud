using System.Security.Claims;
using System.Text.Json;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace GincanaHud.Admin.Services;

/// <summary>
/// The panel's login: the Api's login response (JWT, role, organization, expiry) kept in localStorage,
/// so the session survives closing the tab for as long as the token lasts (12 h, D21).
/// The Api validates the token on every call; here it only drives what the pages show.
/// </summary>
public sealed class AdminSession(IJSRuntime js) : AuthenticationStateProvider
{
	private const string StorageKey = "gincanahud.admin.session";
	private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

	private AdminLoginResponse? _session;
	private bool _loaded;

	public async Task<string?> GetAccessTokenAsync()
	{
		var session = await LoadAsync();
		return session?.AccessToken;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		var session = await LoadAsync();
		return session is null ? Anonymous : new AuthenticationState(ToPrincipal(session));
	}

	public async Task SignInAsync(AdminLoginResponse session)
	{
		_session = session;
		_loaded = true;
		await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(session));
		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(ToPrincipal(session))));
	}

	public async Task SignOutAsync()
	{
		_session = null;
		_loaded = true;
		await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
		NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
	}

	private async Task<AdminLoginResponse?> LoadAsync()
	{
		if (!_loaded)
		{
			_loaded = true;
			var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
			try
			{
				_session = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<AdminLoginResponse>(json);
			}
			catch (JsonException)
			{
				_session = null;
			}
		}

		if (_session is not null && _session.ExpiresAt <= DateTimeOffset.UtcNow)
			await SignOutAsync();

		return _session;
	}

	private static ClaimsPrincipal ToPrincipal(AdminLoginResponse session)
	{
		var role = session.Role?.Trim() ?? "";
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, session.Id.ToString()),
			new(ClaimTypes.Name, session.Username),
			new(ClaimTypes.Role, role)
		};
		if (role == AdminRoles.OrganizationAdmin && session.OrganizationId is Guid orgId)
			claims.Add(new Claim(AdminClaimTypes.OrganizationId, orgId.ToString()));
		if (role == AdminRoles.OrganizationAdmin && !string.IsNullOrWhiteSpace(session.OrganizationName))
			claims.Add(new Claim(AdminClaimTypes.OrganizationName, session.OrganizationName));

		var identity = new ClaimsIdentity(claims, "GincanaHud.Api", ClaimTypes.Name, ClaimTypes.Role);
		return new ClaimsPrincipal(identity);
	}
}
