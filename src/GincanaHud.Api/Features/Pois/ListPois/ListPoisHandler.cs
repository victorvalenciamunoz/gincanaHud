using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Pois.ListPois;

public sealed class ListPoisHandler(AppDbContext db)
	: IRequestHandler<ListPoisQuery, ErrorOr<IReadOnlyList<PoiDto>>>
{
	public async Task<ErrorOr<IReadOnlyList<PoiDto>>> Handle(
		ListPoisQuery request,
		CancellationToken cancellationToken)
	{
		var query = db.Pois.AsNoTracking().AsQueryable();
		if (request.OrganizationId is Guid orgId)
			query = query.Where(p => p.OrganizationId == orgId);

		var pois = await query
			.OrderByDescending(p => p.CreatedAt)
			.Select(p => new PoiDto(
				p.Id, p.OrganizationId, p.Name, p.Latitude, p.Longitude, p.RadiusMeters, p.DefaultPoints, p.Clue))
			.ToListAsync(cancellationToken);

		return pois;
	}
}
