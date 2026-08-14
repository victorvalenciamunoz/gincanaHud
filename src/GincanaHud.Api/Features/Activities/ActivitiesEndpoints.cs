using GincanaHud.Api;
using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Activities.AddActivityPoi;
using GincanaHud.Api.Features.Activities.CapturePoi;
using GincanaHud.Api.Features.Activities.CreateActivity;
using GincanaHud.Api.Features.Activities.GetActivity;
using GincanaHud.Api.Features.Activities.GetActivityByCode;
using GincanaHud.Api.Features.Activities.GetLiveProgress;
using GincanaHud.Api.Features.Activities.GetRanking;
using GincanaHud.Api.Features.Activities.JoinActivity;
using GincanaHud.Api.Features.Activities.ListActivities;
using GincanaHud.Api.Features.Activities.RegisterParticipant;
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

		// Mobile + Admin reads
		group.MapGet("/", async (bool? includeInactive, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new ListActivitiesQuery(includeInactive == true), ct);
			return result.ToHttpResult();
		}).AllowAnonymous();

		group.MapPost("/join", async (JoinActivityRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new JoinActivityCommand(body), ct);
			return result.ToHttpResult();
		}).AllowAnonymous();

		group.MapGet("/by-code/{code}", async (string code, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new GetActivityByCodeQuery(code), ct);
			return result.ToHttpResult();
		}).AllowAnonymous();

		group.MapGet("/{id:guid}", async (Guid id, Guid? userId, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new GetActivityQuery(id, userId), ct);
			return result.ToHttpResult();
		}).AllowAnonymous();

		group.MapPost("/{id:guid}/participants", async (Guid id, RegisterParticipantRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new RegisterParticipantCommand(id, body.UserId), ct);
			return result.ToHttpResult(_ => Results.NoContent());
		}).AllowAnonymous();

		group.MapPost("/{id:guid}/capture", async (Guid id, CaptureRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new CapturePoiCommand(id, body), ct);
			return result.ToHttpResult();
		}).AllowAnonymous();

		group.MapGet("/{id:guid}/ranking", async (Guid id, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new GetRankingQuery(id), ct);
			return result.ToHttpResult();
		}).AllowAnonymous();

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
