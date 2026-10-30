using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.ListActivities;

/// <param name="OrganizationId">Only this organization's activities; <c>null</c> for all (SuperAdmin).</param>
public sealed record ListActivitiesQuery(bool IncludeInactive, Guid? OrganizationId = null)
	: IRequest<ErrorOr<IReadOnlyList<ActivitySummaryDto>>>;
