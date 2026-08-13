using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.RemoveActivityPoi;

public sealed class RemoveActivityPoiHandler(AppDbContext db)
	: IRequestHandler<RemoveActivityPoiCommand, ErrorOr<Deleted>>
{
	public async Task<ErrorOr<Deleted>> Handle(
		RemoveActivityPoiCommand request,
		CancellationToken cancellationToken)
	{
		var link = await db.ActivityPois
			.FirstOrDefaultAsync(
				ap => ap.ActivityId == request.ActivityId && ap.PoiId == request.PoiId,
				cancellationToken);
		if (link is null)
			return Error.NotFound(code: "ActivityPoi.NotFound", description: "POI no pertenece a la actividad.");

		db.ActivityPois.Remove(link);

		var linkedElsewhere = await db.ActivityPois.AnyAsync(
			ap => ap.PoiId == request.PoiId && ap.ActivityId != request.ActivityId,
			cancellationToken);
		if (!linkedElsewhere)
		{
			var hasCaptures = await db.Captures.AnyAsync(c => c.PoiId == request.PoiId, cancellationToken);
			if (!hasCaptures)
			{
				var poi = await db.Pois.FirstOrDefaultAsync(p => p.Id == request.PoiId, cancellationToken);
				if (poi is not null)
					db.Pois.Remove(poi);
			}
		}

		await db.SaveChangesAsync(cancellationToken);
		return Result.Deleted;
	}
}
