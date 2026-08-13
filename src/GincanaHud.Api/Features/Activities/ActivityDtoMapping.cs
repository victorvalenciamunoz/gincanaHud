using GincanaHud.Api.Domain.Activities;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities;

internal static class ActivityDtoMapping
{
	public static ActivitySummaryDto ToSummary(Activity a, string? organizationName = null)
		=> new(
			a.Id,
			a.Title,
			a.Description,
			a.IsActive,
			a.JoinCode,
			a.RouteMode,
			a.StartsAt,
			a.EndsAt,
			a.OrganizationId,
			organizationName ?? a.Organization?.Name);

	public static UserDto ToUserDto(Domain.Users.User u)
		=> new(u.Id, u.DisplayName, u.ContactEmail, u.ContactPhone, u.CreatedAt);
}
