using ErrorOr;
using GincanaHud.Api.Common.Messaging;

namespace GincanaHud.Api.Features.Users.ClearPlayers;

public sealed record ClearPlayersCommand : IRequest<ErrorOr<ClearPlayersResult>>;

public sealed record ClearPlayersResult(int CapturesDeleted, int ParticipantsDeleted, int UsersDeleted);
