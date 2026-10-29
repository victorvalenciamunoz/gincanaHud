using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.AdminAuth.Login;
using GincanaHud.Api.Features.AdminUsers.CreateOrgAdmin;
using GincanaHud.Api.Features.AdminUsers.ListAdminUsers;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.AdminAuth;

// Routes can't start with "admin": the Functions host reserves it and silently drops the function (D21).
public sealed class AdminAuthFunctions(ISender sender)
{
	[Function("AdminLogin")]
	public Task<IResult> Login(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "panel-auth/login")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<AdminLoginRequest>(async body =>
		{
			var result = await sender.Send(new AdminLoginCommand(body.Username, body.Password), ct);
			return result.ToHttpResult();
		}, ct);

	[Function("ListAdminUsers")]
	[Authorize(Policy = JwtAuthExtensions.SuperAdminPolicy)]
	public async Task<IResult> ListAdminUsers(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "panel-users")] HttpRequest req,
		CancellationToken ct)
	{
		var result = await sender.Send(new ListAdminUsersQuery(), ct);
		return result.ToHttpResult();
	}

	[Function("CreateOrgAdmin")]
	[Authorize(Policy = JwtAuthExtensions.SuperAdminPolicy)]
	public Task<IResult> CreateOrgAdmin(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "panel-users")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<CreateOrgAdminRequest>(async body =>
		{
			var result = await sender.Send(
				new CreateOrgAdminCommand(body.Username, body.Password, body.OrganizationId), ct);
			return result.ToHttpResult(u => Results.Created($"/api/panel-users/{u.Id}", u));
		}, ct);
}
