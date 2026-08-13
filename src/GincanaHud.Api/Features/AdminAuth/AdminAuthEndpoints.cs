using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.AdminAuth.Login;
using GincanaHud.Api.Features.AdminUsers.CreateOrgAdmin;
using GincanaHud.Api.Features.AdminUsers.ListAdminUsers;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.AdminAuth;

public static class AdminAuthEndpoints
{
	public static IEndpointRouteBuilder MapAdminAuthEndpoints(this IEndpointRouteBuilder app)
	{
		app.MapPost("/api/admin-auth/login", async (AdminLoginRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new AdminLoginCommand(body.Username, body.Password), ct);
			return result.ToHttpResult();
		});

		var users = app.MapGroup("/api/admin-users");

		users.MapGet("/", async (ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new ListAdminUsersQuery(), ct);
			return result.ToHttpResult();
		});

		users.MapPost("/", async (CreateOrgAdminRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(
				new CreateOrgAdminCommand(body.Username, body.Password, body.OrganizationId), ct);
			return result.ToHttpResult(u => Results.Created($"/api/admin-users/{u.Id}", u));
		});

		return app;
	}
}
