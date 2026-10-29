using GincanaHud.Admin;
using GincanaHud.Admin.Components;
using GincanaHud.Admin.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// In Static Web Apps the Api lives under /api on the same site (D20). Locally the Functions run on their own port.
var apiBase = builder.HostEnvironment.IsDevelopment()
	? "http://localhost:5263/"
	: builder.HostEnvironment.BaseAddress;

builder.Services.AddAuthorizationCore(options =>
	options.AddPolicy("SuperAdmin", p => p.RequireRole(AdminRoles.SuperAdmin)));
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AdminSession>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AdminSession>());
builder.Services.AddScoped(sp => new AdminApiClient(
	new HttpClient(new AdminApiAuthHandler(sp.GetRequiredService<AdminSession>()) { InnerHandler = new HttpClientHandler() })
	{
		BaseAddress = new Uri(apiBase)
	}));

await builder.Build().RunAsync();
