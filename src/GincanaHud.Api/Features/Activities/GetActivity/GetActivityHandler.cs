using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.GetActivity;

public sealed class GetActivityHandler(AppDbContext db)
	: IRequestHandler<GetActivityQuery, ErrorOr<ActivityDetailDto>>
{
	public async Task<ErrorOr<ActivityDetailDto>> Handle(
		GetActivityQuery request,
		CancellationToken cancellationToken)
	{
		var activity = await db.Activities.AsNoTracking()
			.Include(a => a.Organization)
			.Include(a => a.Pois).ThenInclude(ap => ap.Poi)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");

		HashSet<Guid> captured = [];
		Dictionary<Guid, DateTimeOffset> capturedAt = [];
		if (request.UserId is Guid uid)
		{
			var caps = await db.Captures.AsNoTracking()
				.Where(c => c.ActivityId == request.Id && c.UserId == uid)
				.Select(c => new { c.PoiId, c.CapturedAt })
				.ToListAsync(cancellationToken);
			captured = caps.Select(c => c.PoiId).ToHashSet();
			capturedAt = caps.ToDictionary(c => c.PoiId, c => c.CapturedAt);
		}

		return new ActivityDetailDto(
			activity.Id,
			activity.Title,
			activity.Description,
			activity.IsActive,
			activity.JoinCode,
			activity.RouteMode,
			activity.StartsAt,
			activity.EndsAt,
			activity.OrganizationId,
			activity.Organization.Name,
			activity.Pois.OrderBy(ap => ap.Order).Select(ap =>
			{
				var isCaptured = captured.Contains(ap.PoiId);
				return new ActivityPoiDto(
					ap.PoiId,
					ap.Poi.Name,
					ap.Order,
					ap.Poi.Latitude,
					ap.Poi.Longitude,
					ap.Poi.RadiusMeters,
					ap.PointsOverride ?? ap.Poi.DefaultPoints,
					isCaptured,
					isCaptured ? capturedAt[ap.PoiId] : null,
					// La pista guía hacia el POI (el cliente ya recibe lat/lon para el HUD).
					ap.Poi.Clue);
			}).ToList());
	}
}
