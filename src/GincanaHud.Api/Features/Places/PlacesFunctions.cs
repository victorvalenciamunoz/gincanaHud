using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Places.SearchPlaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Places;

[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
public sealed class PlacesFunctions(ISender sender)
{
	[Function("SearchPlaces")]
	public async Task<IResult> Search(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "places")] HttpRequest req,
		CancellationToken ct)
	{
		var result = await sender.Send(new SearchPlacesQuery(req.Query["q"]), ct);
		return result.ToHttpResult();
	}
}
