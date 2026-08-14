using GincanaHud.Api;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Api.Features.Activities;
using GincanaHud.Api.Features.AdminAuth;
using GincanaHud.Api.Features.Organizations;
using GincanaHud.Api.Features.Pois;
using GincanaHud.Api.Features.Users;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("gincanahud");
builder.Services.Configure<AdminBootstrapOptions>(
	builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddGincanaJwtAuth(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();
builder.Services.AddApiMediator();
builder.Services.ConfigureHttpJsonOptions(o =>
	o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
	app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
	var bootstrap = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminBootstrapOptions>>();
	await DbSeeder.SeedAsync(db, hasher, bootstrap, app.Environment);
}

app.MapUsersEndpoints();
app.MapOrganizationsEndpoints();
app.MapPoisEndpoints();
app.MapActivitiesEndpoints();
app.MapAdminAuthEndpoints();

app.Run();
