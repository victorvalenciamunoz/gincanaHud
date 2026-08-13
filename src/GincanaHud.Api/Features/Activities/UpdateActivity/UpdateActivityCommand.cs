using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.UpdateActivity;

public sealed record UpdateActivityCommand(
	Guid Id,
	string Title,
	string Description,
	bool IsActive,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	ActivityRouteMode RouteMode = ActivityRouteMode.Sequential) : IRequest<ErrorOr<ActivitySummaryDto>>;
