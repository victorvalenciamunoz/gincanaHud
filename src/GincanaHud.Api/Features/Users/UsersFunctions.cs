using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Users.ListUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Users;

// Players come in by joining (D25) and are removed per activity (RemovePlayers): here they are only listed.
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
}
