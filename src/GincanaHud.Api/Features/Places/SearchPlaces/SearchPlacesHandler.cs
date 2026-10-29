using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Places.SearchPlaces;

public sealed class SearchPlacesHandler(NominatimClient nominatim)
	: IRequestHandler<SearchPlacesQuery, ErrorOr<IReadOnlyList<GeocodeHitDto>>>
{
	public async Task<ErrorOr<IReadOnlyList<GeocodeHitDto>>> Handle(
		SearchPlacesQuery request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Query))
			return ErrorOrFactory.From<IReadOnlyList<GeocodeHitDto>>([]);

		try
		{
			return ErrorOrFactory.From(await nominatim.SearchAsync(request.Query, cancellationToken));
		}
		catch (HttpRequestException)
		{
			return Error.Failure(
				code: "Places.Unavailable",
				description: "La búsqueda de lugares no responde; inténtalo en un momento.");
		}
	}
}
