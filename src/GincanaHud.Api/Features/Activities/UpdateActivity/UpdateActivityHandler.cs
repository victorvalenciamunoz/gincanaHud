using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.UpdateActivity;

public sealed class UpdateActivityHandler(AppDbContext db)
	: IRequestHandler<UpdateActivityCommand, ErrorOr<ActivitySummaryDto>>
{
	public async Task<ErrorOr<ActivitySummaryDto>> Handle(
		UpdateActivityCommand request,
		CancellationToken cancellationToken)
	{
		var activity = await db.Activities
			.Include(a => a.Organization)
			.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");

		var updated = activity.Update(
			request.Title,
			request.Description,
			request.IsActive,
			request.StartsAt,
			request.EndsAt,
			request.RouteMode);
		if (updated.IsError)
			return updated.Errors;

		await db.SaveChangesAsync(cancellationToken);
		return ActivityDtoMapping.ToSummary(activity, activity.Organization.Name);
	}
}
