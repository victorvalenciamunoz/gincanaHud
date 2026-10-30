using ErrorOr;
using GincanaHud.Api.Common.Messaging;

namespace GincanaHud.Api.Features.Activities.RemovePlayers;

/// <summary>Removes one player (<paramref name="UserId"/>) or all of them from an activity.</summary>
public sealed record RemovePlayersCommand(Guid ActivityId, Guid? UserId = null, Guid? OrganizationScope = null)
	: IRequest<ErrorOr<int>>;
