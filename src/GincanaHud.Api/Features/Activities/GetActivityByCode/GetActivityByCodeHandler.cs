using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.ValueObjects;
using GincanaHud.Api.Features.Activities;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.GetActivityByCode;

public sealed class GetActivityByCodeHandler(AppDbContext db)
	: IRequestHandler<GetActivityByCodeQuery, ErrorOr<ActivitySummaryDto>>
{
	public async Task<ErrorOr<ActivitySummaryDto>> Handle(
		GetActivityByCodeQuery request,
		CancellationToken cancellationToken)
	{
		var codeResult = JoinCode.Create(request.JoinCode);
		if (codeResult.IsError)
			return codeResult.Errors;

		var activity = await db.Activities.AsNoTracking()
			.Include(a => a.Organization)
			.FirstOrDefaultAsync(a => a.JoinCode == codeResult.Value.Value, cancellationToken);

		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "No hay actividad con ese código.");

		return ActivityDtoMapping.ToSummary(activity, activity.Organization.Name);
	}
}
