using GincanaHud.Api;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker.Builder;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("gincanahud");
builder.Services.Configure<AdminBootstrapOptions>(
	builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddGincanaJwtAuth(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddApiMediator();
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
