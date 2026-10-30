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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Activities;

public sealed class ActivitiesFunctions(ISender sender)
{
	// Panel only: it hands out every activity id, and the app joins by code (D24).
	[Function("ListActivities")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public async Task<IResult> List(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities")] HttpRequest req,
		CancellationToken ct)
	{
		var includeInactive = bool.TryParse(req.Query["includeInactive"], out var value) && value;
		var result = await sender.Send(new ListActivitiesQuery(includeInactive, AdminScope.OrganizationOf(req)), ct);
		return result.ToHttpResult();
	}

	// Mobile reads (anonymous)
	[Function("JoinActivity")]
	public Task<IResult> Join(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities/join")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<JoinActivityRequest>(async body =>
		{
			var result = await sender.Send(new JoinActivityCommand(body), ct);
			return result.ToHttpResult();
		}, ct);

	[Function("GetActivityByCode")]
	public async Task<IResult> GetByCode(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities/by-code/{code}")] HttpRequest req,
		string code,
		CancellationToken ct)
	{
		var result = await sender.Send(new GetActivityByCodeQuery(code), ct);
		return result.ToHttpResult();
	}

	[Function("GetActivity")]
	public async Task<IResult> Get(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities/{id:guid}")] HttpRequest req,
		Guid id,
		CancellationToken ct)
	{
		Guid? userId = Guid.TryParse(req.Query["userId"], out var value) ? value : null;
		var result = await sender.Send(new GetActivityQuery(id, userId), ct);
		return result.ToHttpResult();
	}

	[Function("RegisterParticipant")]
	public Task<IResult> RegisterParticipant(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities/{id:guid}/participants")] HttpRequest req,
		Guid id,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<RegisterParticipantRequest>(async body =>
		{
			var result = await sender.Send(new RegisterParticipantCommand(id, body.UserId), ct);
			return result.ToHttpResult(_ => Results.NoContent());
		}, ct);

	[Function("CapturePoi")]
	public Task<IResult> Capture(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities/{id:guid}/capture")] HttpRequest req,
		Guid id,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<CaptureRequest>(async body =>
		{
			var result = await sender.Send(new CapturePoiCommand(id, body), ct);
			return result.ToHttpResult();
		}, ct);

	// The app shows it when the player finishes: no contacts (D24) and only the caller's own id (D25).
	[Function("GetRanking")]
	public async Task<IResult> GetRanking(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities/{id:guid}/ranking")] HttpRequest req,
		Guid id,
		CancellationToken ct)
	{
		Guid? viewerId = Guid.TryParse(req.Query["userId"], out var value) ? value : null;
		var result = await sender.Send(new GetRankingQuery(id, ViewerId: viewerId), ct);
		return result.ToHttpResult();
	}

	// The panel's ranking, with contacts for the prizes: only the activity's organization.
	[Function("GetRankingWithContacts")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public async Task<IResult> GetRankingWithContacts(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities/{id:guid}/ranking/contacts")] HttpRequest req,
		Guid id,
		CancellationToken ct)
	{
		var result = await sender.Send(new GetRankingQuery(id, IncludeContacts: true, AdminScope.OrganizationOf(req)), ct);
		return result.ToHttpResult();
	}

	// Admin writes / live board
	[Function("CreateActivity")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public Task<IResult> Create(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<CreateActivityRequest>(async body =>
		{
			var result = await sender.Send(new CreateActivityCommand(
				body.OrganizationId,
				body.Title,
				body.Description,
				body.StartsAt,
				body.EndsAt,
				body.RouteMode,
				AdminScope.OrganizationOf(req)), ct);
			return result.ToHttpResult(a => Results.Created($"/api/activities/{a.Id}", a));
		}, ct);

	[Function("UpdateActivity")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public Task<IResult> Update(
		[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "activities/{id:guid}")] HttpRequest req,
		Guid id,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<UpdateActivityRequest>(async body =>
		{
			var result = await sender.Send(
				new UpdateActivityCommand(
					id, body.Title, body.Description, body.IsActive, body.StartsAt, body.EndsAt, body.RouteMode,
					AdminScope.OrganizationOf(req)), ct);
			return result.ToHttpResult();
		}, ct);

	[Function("AddActivityPoi")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public Task<IResult> AddPoi(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities/{id:guid}/pois")] HttpRequest req,
		Guid id,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<CreatePoiRequest>(async body =>
		{
			var result = await sender.Send(new AddActivityPoiCommand(id, body, AdminScope.OrganizationOf(req)), ct);
			return result.ToHttpResult(p => Results.Created($"/api/activities/{id}/pois/{p.PoiId}", p));
		}, ct);

	[Function("UpdateActivityPoi")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public Task<IResult> UpdatePoi(
		[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "activities/{id:guid}/pois/{poiId:guid}")] HttpRequest req,
		Guid id,
		Guid poiId,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<UpdateActivityPoiRequest>(async body =>
		{
			var result = await sender.Send(new UpdateActivityPoiCommand(id, poiId, body, AdminScope.OrganizationOf(req)), ct);
			return result.ToHttpResult();
		}, ct);

	[Function("RemoveActivityPoi")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public async Task<IResult> RemovePoi(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "activities/{id:guid}/pois/{poiId:guid}")] HttpRequest req,
		Guid id,
		Guid poiId,
		CancellationToken ct)
	{
		var result = await sender.Send(new RemoveActivityPoiCommand(id, poiId, AdminScope.OrganizationOf(req)), ct);
		return result.ToHttpResult(_ => Results.NoContent());
	}

	[Function("GetLiveProgress")]
	[Authorize(Policy = JwtAuthExtensions.AdminPolicy)]
	public async Task<IResult> GetLive(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities/{id:guid}/live")] HttpRequest req,
		Guid id,
		CancellationToken ct)
	{
		var result = await sender.Send(new GetLiveProgressQuery(id, AdminScope.OrganizationOf(req)), ct);
		return result.ToHttpResult();
	}
}
