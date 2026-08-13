using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.UpdateActivityPoi;

public sealed class UpdateActivityPoiHandler(AppDbContext db)
	: IRequestHandler<UpdateActivityPoiCommand, ErrorOr<ActivityPoiDto>>
{
	public async Task<ErrorOr<ActivityPoiDto>> Handle(
		UpdateActivityPoiCommand request,
		CancellationToken cancellationToken)
	{
		var link = await db.ActivityPois
			.Include(ap => ap.Poi)
			.Include(ap => ap.Activity).ThenInclude(a => a.Pois)
			.FirstOrDefaultAsync(
				ap => ap.ActivityId == request.ActivityId && ap.PoiId == request.PoiId,
				cancellationToken);

		if (link is null)
			return Error.NotFound(code: "ActivityPoi.NotFound", description: "POI no pertenece a la actividad.");

		var body = request.Request;
		var updated = link.Poi.Update(
			body.Name, body.Latitude, body.Longitude, body.RadiusMeters, body.Clue, body.Points);
		if (updated.IsError)
			return updated.Errors;

		var newOrder = body.Order > 0 ? body.Order : link.Order;
		if (newOrder != link.Order)
		{
			var orderResult = link.ChangeOrder(newOrder, link.Activity.Pois);
			if (orderResult.IsError)
				return orderResult.Errors;
		}

		link.ClearPointsOverride();
		await db.SaveChangesAsync(cancellationToken);

		var p = link.Poi;
		return new ActivityPoiDto(
			link.PoiId, p.Name, link.Order, p.Latitude, p.Longitude, p.RadiusMeters, p.DefaultPoints,
			Captured: false, CapturedAt: null, Clue: p.Clue);
	}
}
