using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.CreateActivity;

public sealed record CreateActivityCommand(
	Guid OrganizationId,
	string Title,
	string Description,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	ActivityRouteMode RouteMode = ActivityRouteMode.Sequential) : IRequest<ErrorOr<ActivitySummaryDto>>;
