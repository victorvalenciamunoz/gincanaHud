using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.GetLiveProgress;

public sealed class GetLiveProgressHandler(AppDbContext db)
	: IRequestHandler<GetLiveProgressQuery, ErrorOr<LiveProgressDto>>
{
	public async Task<ErrorOr<LiveProgressDto>> Handle(
		GetLiveProgressQuery request,
		CancellationToken cancellationToken)
	{
		var activity = await db.Activities.AsNoTracking()
			.Include(a => a.Pois)
			.ThenInclude(ap => ap.Poi)
			.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");

		var pois = activity.Pois
			.OrderBy(ap => ap.Order)
			.Select(ap => new { ap.PoiId, ap.Order, Name = ap.Poi.Name })
			.ToList();
		var poiTotal = pois.Count;
		var lastPoiId = pois.LastOrDefault()?.PoiId;
		var isFree = activity.RouteMode == ActivityRouteMode.Free;

		var participants = await db.ActivityParticipants.AsNoTracking()
			.Where(p => p.ActivityId == request.ActivityId)
			.Select(p => new { p.UserId, p.JoinedAt })
			.ToListAsync(cancellationToken);

		var userIds = participants.Select(p => p.UserId).ToList();
		var users = await db.Users.AsNoTracking()
			.Where(u => userIds.Contains(u.Id))
			.ToDictionaryAsync(u => u.Id, cancellationToken);

		var captures = await db.Captures.AsNoTracking()
			.Where(c => c.ActivityId == request.ActivityId)
			.Select(c => new { c.UserId, c.PoiId, c.CapturedAt })
			.ToListAsync(cancellationToken);

		var capturesByUser = captures.GroupBy(c => c.UserId)
			.ToDictionary(g => g.Key, g => g.ToList());

		var players = new List<LivePlayerProgressDto>();

		foreach (var part in participants)
		{
			users.TryGetValue(part.UserId, out var user);
			capturesByUser.TryGetValue(part.UserId, out var userCaps);
			userCaps ??= [];

			var capturedPoiIds = userCaps.Select(c => c.PoiId).ToHashSet();
			var capturedCount = capturedPoiIds.Count;
			var lastCaptureAt = userCaps.Count == 0
				? (DateTimeOffset?)null
				: userCaps.Max(c => c.CapturedAt);

			DateTimeOffset? finishedAt = null;
			if (poiTotal > 0 && capturedCount >= poiTotal)
			{
				if (!isFree && lastPoiId is Guid lastId)
				{
					var finish = userCaps.FirstOrDefault(c => c.PoiId == lastId);
					finishedAt = finish?.CapturedAt ?? lastCaptureAt;
				}
				else
				{
					finishedAt = lastCaptureAt;
				}
			}

			int? currentOrder = null;
			string? currentPoiName = null;
			string status;
			if (finishedAt is not null)
			{
				status = "Meta";
			}
			else if (isFree)
			{
				status = capturedCount == 0 ? "En salida" : "Ruta libre";
				currentPoiName = "Más cercano";
			}
			else
			{
				var next = pois.FirstOrDefault(p => !capturedPoiIds.Contains(p.PoiId));
				if (next is null)
				{
					status = poiTotal == 0 ? "Sin ruta" : "Meta";
				}
				else
				{
					currentOrder = next.Order;
					currentPoiName = next.Name;
					status = capturedCount == 0 ? "En salida" : $"Hacia #{next.Order}";
				}
			}

			players.Add(new LivePlayerProgressDto(
				part.UserId,
				user?.DisplayName ?? "?",
				user?.ContactEmail,
				user?.ContactPhone,
				capturedCount,
				poiTotal,
				currentOrder,
				currentPoiName,
				lastCaptureAt,
				finishedAt,
				FinishPlace: null,
				status,
				part.JoinedAt));
		}

		players = players
			.OrderBy(p => p.FinishedAt is null)
			.ThenBy(p => p.FinishedAt)
			.ThenByDescending(p => p.CapturedCount)
			.ThenBy(p => p.LastCaptureAt ?? DateTimeOffset.MaxValue)
			.ThenBy(p => p.JoinedAt)
			.ToList();

		var place = 0;
		players = players.Select(p =>
		{
			if (p.FinishedAt is null)
				return p;
			place++;
			return p with { FinishPlace = place };
		}).ToList();

		return new LiveProgressDto(
			activity.Id,
			activity.Title,
			activity.JoinCode,
			activity.RouteMode,
			activity.StartsAt,
			activity.EndsAt,
			poiTotal,
			players.Count,
			players.Count(p => p.FinishedAt is not null),
			DateTimeOffset.UtcNow,
			players);
	}
}
