using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Pois;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Pois.CreatePoi;

public sealed class CreatePoiHandler(AppDbContext db)
	: IRequestHandler<CreatePoiCommand, ErrorOr<PoiDto>>
{
	public async Task<ErrorOr<PoiDto>> Handle(CreatePoiCommand request, CancellationToken cancellationToken)
	{
		var orgExists = await db.Organizations.AnyAsync(o => o.Id == request.OrganizationId, cancellationToken);
		if (!orgExists)
			return Error.NotFound(code: "Organization.NotFound", description: "Organización no encontrada.");

		var poi = Poi.Create(
			request.OrganizationId,
			request.Name,
			request.Latitude,
			request.Longitude,
			request.RadiusMeters,
			request.Clue,
			request.Points);

		if (poi.IsError)
			return poi.Errors;

		db.Pois.Add(poi.Value);
		await db.SaveChangesAsync(cancellationToken);

		var p = poi.Value;
		return new PoiDto(p.Id, p.OrganizationId, p.Name, p.Latitude, p.Longitude, p.RadiusMeters, p.DefaultPoints, p.Clue);
	}
}
