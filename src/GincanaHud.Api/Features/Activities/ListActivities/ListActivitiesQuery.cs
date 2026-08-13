using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.ListActivities;

public sealed record ListActivitiesQuery(bool IncludeInactive)
	: IRequest<ErrorOr<IReadOnlyList<ActivitySummaryDto>>>;
