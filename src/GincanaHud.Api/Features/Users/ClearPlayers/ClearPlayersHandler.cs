using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Users.ClearPlayers;

public sealed class ClearPlayersHandler(AppDbContext db)
	: IRequestHandler<ClearPlayersCommand, ErrorOr<ClearPlayersResult>>
{
	public async Task<ErrorOr<ClearPlayersResult>> Handle(
		ClearPlayersCommand request,
		CancellationToken cancellationToken)
	{
		// Orden por FKs: capturas → participantes → usuarios jugadores.
		var captures = await db.Captures.ExecuteDeleteAsync(cancellationToken);
		var participants = await db.ActivityParticipants.ExecuteDeleteAsync(cancellationToken);
		var users = await db.Users.ExecuteDeleteAsync(cancellationToken);

		return new ClearPlayersResult(captures, participants, users);
	}
}
