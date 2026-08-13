using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Pois.ListPois;

public sealed record ListPoisQuery(Guid? OrganizationId = null)
	: IRequest<ErrorOr<IReadOnlyList<PoiDto>>>;
