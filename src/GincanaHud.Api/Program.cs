using GincanaHud.Api;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Api.Features.Places;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker.Builder;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.UseMiddleware<JwtAuthorizationMiddleware>();
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("gincanahud");
builder.Services.Configure<AdminBootstrapOptions>(
	builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddGincanaJwtAuth(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddApiMediator();
builder.Services.AddMemoryCache();
var nominatim = builder.Services.AddHttpClient<NominatimClient>(client =>
{
	client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
	client.DefaultRequestHeaders.UserAgent.ParseAdd(
		"GincanaHud/0.1 (+https://github.com/victorvalenciamunoz/gincanaHud)");
});
// No automatic retries from ServiceDefaults: they would skip the one-request-per-second limit.
// The API is marked experimental (EXTEXP0001); there is no stable way to opt one client out.
#pragma warning disable EXTEXP0001
nominatim.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
builder.Services.ConfigureHttpJsonOptions(o =>
	o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
	var bootstrap = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminBootstrapOptions>>();
	var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
	await DbSeeder.SeedAsync(db, hasher, bootstrap, environment);
}

app.Run();
