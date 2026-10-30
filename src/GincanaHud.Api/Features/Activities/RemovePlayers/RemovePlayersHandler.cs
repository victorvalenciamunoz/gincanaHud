using ErrorOr;
using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.RemovePlayers;

public sealed class RemovePlayersHandler(AppDbContext db)
	: IRequestHandler<RemovePlayersCommand, ErrorOr<int>>
{
	public async Task<ErrorOr<int>> Handle(
		RemovePlayersCommand request,
		CancellationToken cancellationToken)
	{
		var activity = await db.Activities.AsNoTracking()
			.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);
		if (activity is null || request.OrganizationScope.Excludes(activity.OrganizationId))
			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");

		var userIds = await db.ActivityParticipants.AsNoTracking()
			.Where(p => p.ActivityId == request.ActivityId && (request.UserId == null || p.UserId == request.UserId))
			.Select(p => p.UserId)
			.ToListAsync(cancellationToken);
		if (request.UserId is not null && userIds.Count == 0)
			return Error.NotFound(code: "Player.NotFound", description: "Ese jugador no está en la actividad.");

		// Retries are on (Aspire), so the transaction runs inside the execution strategy.
		await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
		{
			await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
			await db.Captures
				.Where(c => c.ActivityId == request.ActivityId && userIds.Contains(c.UserId))
				.ExecuteDeleteAsync(cancellationToken);
			await db.ActivityParticipants
				.Where(p => p.ActivityId == request.ActivityId && userIds.Contains(p.UserId))
				.ExecuteDeleteAsync(cancellationToken);
			// A player is one join (D25); those from before it may still be in another activity, and stay.
			await db.Users
				.Where(u => userIds.Contains(u.Id) && !u.Participations.Any() && !u.Captures.Any())
				.ExecuteDeleteAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		});

		return userIds.Count;
	}
}
