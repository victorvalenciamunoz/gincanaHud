using GincanaHud.Api;
using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Activities.AddActivityPoi;
using GincanaHud.Api.Features.Activities.CreateActivity;
using GincanaHud.Api.Features.Activities.GetLiveProgress;
using GincanaHud.Api.Features.Activities.RemoveActivityPoi;
using GincanaHud.Api.Features.Activities.UpdateActivity;
using GincanaHud.Api.Features.Activities.UpdateActivityPoi;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities;

public static class ActivitiesEndpoints
{
	public static RouteGroupBuilder MapActivitiesEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/activities");

		// Admin writes / live board
		group.MapPost("/", async (CreateActivityRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new CreateActivityCommand(
				body.OrganizationId,
				body.Title,
				body.Description,
				body.StartsAt,
				body.EndsAt,
				body.RouteMode), ct);
			return result.ToHttpResult(a => Results.Created($"/api/activities/{a.Id}", a));
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		group.MapPut("/{id:guid}", async (Guid id, UpdateActivityRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(
				new UpdateActivityCommand(
					id, body.Title, body.Description, body.IsActive, body.StartsAt, body.EndsAt, body.RouteMode), ct);
			return result.ToHttpResult();
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		group.MapPost("/{id:guid}/pois", async (Guid id, CreatePoiRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new AddActivityPoiCommand(id, body), ct);
			return result.ToHttpResult(p => Results.Created($"/api/activities/{id}/pois/{p.PoiId}", p));
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		group.MapPut("/{id:guid}/pois/{poiId:guid}", async (
			Guid id, Guid poiId, UpdateActivityPoiRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new UpdateActivityPoiCommand(id, poiId, body), ct);
			return result.ToHttpResult();
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		group.MapDelete("/{id:guid}/pois/{poiId:guid}", async (Guid id, Guid poiId, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new RemoveActivityPoiCommand(id, poiId), ct);
			return result.ToHttpResult(_ => Results.NoContent());
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		group.MapGet("/{id:guid}/live", async (Guid id, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new GetLiveProgressQuery(id), ct);
			return result.ToHttpResult();
		}).RequireAuthorization(JwtAuthExtensions.AdminPolicy);

		return group;
	}
}
