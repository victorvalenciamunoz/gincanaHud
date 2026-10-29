using GincanaHud.Api;
using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Users.ClearPlayers;
using GincanaHud.Api.Features.Users.ListUsers;
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
