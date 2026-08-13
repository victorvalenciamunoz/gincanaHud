using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Pois;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.AddActivityPoi;

public sealed class AddActivityPoiHandler(AppDbContext db)
	: IRequestHandler<AddActivityPoiCommand, ErrorOr<ActivityPoiDto>>
{
	public async Task<ErrorOr<ActivityPoiDto>> Handle(
		AddActivityPoiCommand request,
		CancellationToken cancellationToken)
	{
		var activity = await db.Activities
			.Include(a => a.Pois)
			.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);
		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");

		var nextOrder = activity.Pois.Count == 0 ? 0 : activity.Pois.Max(p => p.Order);
		var order = nextOrder + 1;
		var body = request.Request;

		if (body.OrganizationId != Guid.Empty && body.OrganizationId != activity.OrganizationId)
		{
			return Error.Validation(
				code: "Poi.OrganizationMismatch",
				description: "El POI debe pertenecer a la misma organización que la actividad.");
		}

		var poi = Poi.Create(
			activity.OrganizationId,
			body.Name,
			body.Latitude,
			body.Longitude,
			body.RadiusMeters,
			body.Clue,
			body.Points,
			nameFallback: $"Punto {order}",
			radiusFallback: 12);

		if (poi.IsError)
			return poi.Errors;

		db.Pois.Add(poi.Value);

		var link = activity.AssignPoi(poi.Value.Id, order);
		if (link.IsError)
			return link.Errors;

		await db.SaveChangesAsync(cancellationToken);

		var p = poi.Value;
		return new ActivityPoiDto(
			p.Id, p.Name, order, p.Latitude, p.Longitude, p.RadiusMeters, p.DefaultPoints,
			Captured: false, CapturedAt: null, Clue: p.Clue);
	}
}
