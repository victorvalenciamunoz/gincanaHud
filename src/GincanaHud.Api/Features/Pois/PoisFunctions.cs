using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Pois.CreatePoi;
using GincanaHud.Api.Features.Pois.ListPois;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Pois;

[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
public sealed class PoisFunctions(ISender sender)
{
	[Function("ListPois")]
	public async Task<IResult> List(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pois")] HttpRequest req,
		CancellationToken ct)
	{
		Guid? organizationId = Guid.TryParse(req.Query["organizationId"], out var value) ? value : null;
		var result = await sender.Send(new ListPoisQuery(AdminScope.Restrict(req, organizationId)), ct);
		return result.ToHttpResult();
	}

	[Function("CreatePoi")]
	public Task<IResult> Create(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pois")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<CreatePoiRequest>(async body =>
		{
			var result = await sender.Send(new CreatePoiCommand(
				body.OrganizationId,
				body.Name,
				body.Latitude,
				body.Longitude,
				body.RadiusMeters,
				body.Clue,
				body.Points,
				AdminScope.OrganizationOf(req)), ct);
			return result.ToHttpResult(poi => Results.Created($"/api/pois/{poi.Id}", poi));
		}, ct);
}
