using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.UpdateActivityPoi;

public sealed record UpdateActivityPoiCommand(
	Guid ActivityId,
	Guid PoiId,
	UpdateActivityPoiRequest Request) : IRequest<ErrorOr<ActivityPoiDto>>;
