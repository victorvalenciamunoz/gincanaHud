using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Activities.CapturePoi;
using GincanaHud.Api.Features.Activities.GetActivity;
using GincanaHud.Api.Features.Activities.GetActivityByCode;
using GincanaHud.Api.Features.Activities.GetRanking;
using GincanaHud.Api.Features.Activities.JoinActivity;
using GincanaHud.Api.Features.Activities.ListActivities;
using GincanaHud.Api.Features.Activities.RegisterParticipant;
using GincanaHud.Shared;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Activities;

// Mobile + Admin reads (anonymous).
public sealed class ActivitiesFunctions(ISender sender)
{
	[Function("ListActivities")]
	public async Task<IResult> List(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities")] HttpRequest req,
		CancellationToken ct)
	{
		var includeInactive = bool.TryParse(req.Query["includeInactive"], out var value) && value;
		var result = await sender.Send(new ListActivitiesQuery(includeInactive), ct);
		return result.ToHttpResult();
	}

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

	[Function("GetRanking")]
	public async Task<IResult> GetRanking(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities/{id:guid}/ranking")] HttpRequest req,
		Guid id,
		CancellationToken ct)
	{
		var result = await sender.Send(new GetRankingQuery(id), ct);
		return result.ToHttpResult();
	}
}
