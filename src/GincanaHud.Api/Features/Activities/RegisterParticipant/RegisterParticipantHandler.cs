using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.RegisterParticipant;

public sealed class RegisterParticipantHandler(AppDbContext db)
	: IRequestHandler<RegisterParticipantCommand, ErrorOr<Success>>
{
	public async Task<ErrorOr<Success>> Handle(
		RegisterParticipantCommand request,
		CancellationToken cancellationToken)
	{
		var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
		if (!userExists)
			return Error.NotFound(code: "User.NotFound", description: "Usuario no encontrado.");

		var activity = await db.Activities
			.Include(a => a.Participants)
			.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);
		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");

		if (activity.Participants.Any(p => p.UserId == request.UserId))
			return Result.Success;

		var joined = activity.RegisterParticipant(request.UserId, DateTimeOffset.UtcNow);
		if (joined.IsError)
			return joined.Errors;

		await db.SaveChangesAsync(cancellationToken);
		return Result.Success;
	}
}
