using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Users.UpsertUser;
using GincanaHud.Shared;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Users;

public sealed class UsersFunctions(ISender sender)
{
	[Function("UpsertUser")]
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
}
