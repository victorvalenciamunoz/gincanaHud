using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Activities;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.CreateActivity;

public sealed class CreateActivityHandler(AppDbContext db)
	: IRequestHandler<CreateActivityCommand, ErrorOr<ActivitySummaryDto>>
{
	public async Task<ErrorOr<ActivitySummaryDto>> Handle(
		CreateActivityCommand request,
		CancellationToken cancellationToken)
	{
		var org = await db.Organizations.AsNoTracking()
			.FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);
		if (org is null)
			return Error.NotFound(code: "Organization.NotFound", description: "Organización no encontrada.");

		Activity? created = null;
		for (var attempt = 0; attempt < 5; attempt++)
		{
			var activity = Activity.Create(
				request.OrganizationId,
				request.Title,
				request.Description,
				request.StartsAt,
				request.EndsAt,
				routeMode: request.RouteMode);
			if (activity.IsError)
				return activity.Errors;

			var codeTaken = await db.Activities.AnyAsync(a => a.JoinCode == activity.Value.JoinCode, cancellationToken);
			if (codeTaken)
				continue;

			created = activity.Value;
			break;
		}

		if (created is null)
			return Error.Conflict(code: "JoinCode.Collision", description: "No se pudo generar un código único.");

		db.Activities.Add(created);
		await db.SaveChangesAsync(cancellationToken);
		return ActivityDtoMapping.ToSummary(created, org.Name);
	}
}
