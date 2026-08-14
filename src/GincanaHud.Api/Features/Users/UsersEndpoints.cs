using GincanaHud.Api;
using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Users.ClearPlayers;
using GincanaHud.Api.Features.Users.ListUsers;
using GincanaHud.Api.Features.Users.UpsertUser;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Users;

public static class UsersEndpoints
{
	public static RouteGroupBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/users");

		group.MapGet("/", async (Guid? organizationId, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new ListUsersQuery(organizationId), ct);
			return result.ToHttpResult();
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		group.MapPost("/", async (UpsertUserRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(
				new UpsertUserCommand(body.DisplayName, body.ContactEmail, body.ContactPhone), ct);
			return result.ToHttpResult(value => value.Created
				? Results.Created($"/api/users/{value.User.Id}", value.User)
				: Results.Ok(value.User));
		}).AllowAnonymous();

		// Dev/ops: vaciar jugadores (no toca AdminUsers / orgs / actividades / POIs).
		group.MapPost("/clear-players", async (ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new ClearPlayersCommand(), ct);
			return result.ToHttpResult(r => Results.Ok(new ClearPlayersResultDto(
				r.CapturesDeleted, r.ParticipantsDeleted, r.UsersDeleted)));
		}).RequireAuthorization(JwtAuthExtensions.SuperAdminPolicy);

		return group;
	}
}
