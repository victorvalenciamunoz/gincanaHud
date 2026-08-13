using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Pois.CreatePoi;
using GincanaHud.Api.Features.Pois.ListPois;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Pois;

public static class PoisEndpoints
{
	public static RouteGroupBuilder MapPoisEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/pois");

		group.MapGet("/", async (Guid? organizationId, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new ListPoisQuery(organizationId), ct);
			return result.ToHttpResult();
		});

		group.MapPost("/", async (CreatePoiRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new CreatePoiCommand(
				body.OrganizationId,
				body.Name,
				body.Latitude,
				body.Longitude,
				body.RadiusMeters,
				body.Clue,
				body.Points), ct);
			return result.ToHttpResult(poi => Results.Created($"/api/pois/{poi.Id}", poi));
		});

		return group;
	}
}
