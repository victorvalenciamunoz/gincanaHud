using System.Security.Claims;
using GincanaHud.Admin;
using GincanaHud.Admin.Components;
using GincanaHud.Admin.Services;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/login";
		options.LogoutPath = "/account/logout";
		options.AccessDeniedPath = "/login";
		options.SlidingExpiration = true;
		options.ExpireTimeSpan = TimeSpan.FromHours(12);
	});

builder.Services.AddAuthorization(options =>
{
	options.FallbackPolicy = new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.Build();
	options.AddPolicy("SuperAdmin", p => p.RequireRole(AdminRoles.SuperAdmin));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddHttpClient<AdminApiClient>(client =>
{
	client.BaseAddress = new Uri("https+http://api");
});

builder.Services.AddHttpClient<GeocodeService>(client =>
{
	client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
	client.DefaultRequestHeaders.UserAgent.ParseAdd("GincanaHud.Admin/0.1 (side-project; contact=local)");
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Estáticos sin auth: si no, /login carga sin CSS/JS (FallbackPolicy).
app.MapStaticAssets().AllowAnonymous();

app.MapPost("/account/login", async (HttpContext http, AdminApiClient api) =>
{
	var form = await http.Request.ReadFormAsync();
	var username = form["username"].ToString();
	var password = form["password"].ToString();
	var returnUrl = form["returnUrl"].ToString();

	AdminLoginResponse? session;
	try
	{
		session = await api.LoginAsync(username, password);
	}
	catch
	{
		return Results.Redirect("/login?failed=1");
	}

	if (session is null)
	{
		var fail = "/login?failed=1";
		if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/'))
			fail += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
		return Results.Redirect(fail);
	}

	var role = session.Role?.Trim() ?? "";
	if (role is not (AdminRoles.SuperAdmin or AdminRoles.OrganizationAdmin))
		return Results.Redirect("/login?failed=1");

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

	var identity = new ClaimsIdentity(
		claims,
		CookieAuthenticationDefaults.AuthenticationScheme,
		ClaimTypes.Name,
		ClaimTypes.Role);
	await http.SignInAsync(
		CookieAuthenticationDefaults.AuthenticationScheme,
		new ClaimsPrincipal(identity));

	var target = !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/')
		? returnUrl
		: "/";
	return Results.Redirect(target);
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/account/logout", async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Redirect("/login");
}).AllowAnonymous();

app.MapPost("/account/logout", async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Redirect("/login");
}).AllowAnonymous().DisableAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
