using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Pois.CreatePoi;

public sealed record CreatePoiCommand(
	Guid OrganizationId,
	string Name,
	double Latitude,
	double Longitude,
	double RadiusMeters,
	string Clue,
	int Points) : IRequest<ErrorOr<PoiDto>>;
