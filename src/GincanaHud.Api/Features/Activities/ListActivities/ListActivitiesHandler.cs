using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.ListActivities;

public sealed class ListActivitiesHandler(AppDbContext db)
	: IRequestHandler<ListActivitiesQuery, ErrorOr<IReadOnlyList<ActivitySummaryDto>>>
{
	public async Task<ErrorOr<IReadOnlyList<ActivitySummaryDto>>> Handle(
		ListActivitiesQuery request,
		CancellationToken cancellationToken)
	{
		var query = db.Activities.AsNoTracking().Include(a => a.Organization).AsQueryable();
		if (!request.IncludeInactive)
			query = query.Where(a => a.IsActive);

		var items = await query
			.OrderByDescending(a => a.StartsAt)
			.ThenBy(a => a.Title)
			.ToListAsync(cancellationToken);

		return items.Select(a => ActivityDtoMapping.ToSummary(a, a.Organization.Name)).ToList();
	}
}
