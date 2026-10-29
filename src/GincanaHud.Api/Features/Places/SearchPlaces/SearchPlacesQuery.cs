using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Places.SearchPlaces;

public sealed record SearchPlacesQuery(string? Query)
	: IRequest<ErrorOr<IReadOnlyList<GeocodeHitDto>>>;
