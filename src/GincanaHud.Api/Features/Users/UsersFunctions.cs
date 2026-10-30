using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Users.ClearPlayers;
using GincanaHud.Api.Features.Users.ListUsers;
using GincanaHud.Api.Features.Users.UpsertUser;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Users;

public sealed class UsersFunctions(ISender sender)
{
	[Function("ListUsers")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public async Task<IResult> List(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest req,
		CancellationToken ct)
	{
		Guid? organizationId = Guid.TryParse(req.Query["organizationId"], out var value) ? value : null;
		var result = await sender.Send(new ListUsersQuery(AdminScope.Restrict(req, organizationId)), ct);
		return result.ToHttpResult();
	}

	// Panel only: given an existing name, it answers that player's contact (D24).
	[Function("UpsertUser")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public Task<IResult> Upsert(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<UpsertUserRequest>(async body =>
		{
			var result = await sender.Send(
				new UpsertUserCommand(body.DisplayName, body.ContactEmail, body.ContactPhone), ct);
			return result.ToHttpResult(value => value.Created
				? Results.Created($"/api/users/{value.User.Id}", value.User)
				: Results.Ok(value.User));
		}, ct);

	// Dev/ops: vaciar jugadores (no toca AdminUsers / orgs / actividades / POIs).
	[Function("ClearPlayers")]
	[Authorize(Policy = JwtAuthExtensions.SuperAdminPolicy)]
	public async Task<IResult> ClearPlayers(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/clear-players")] HttpRequest req,
		CancellationToken ct)
	{
		var result = await sender.Send(new ClearPlayersCommand(), ct);
		return result.ToHttpResult(r => Results.Ok(new ClearPlayersResultDto(
			r.CapturesDeleted, r.ParticipantsDeleted, r.UsersDeleted)));
	}
}
