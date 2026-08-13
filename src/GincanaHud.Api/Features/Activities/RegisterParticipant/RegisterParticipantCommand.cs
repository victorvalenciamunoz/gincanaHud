using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.RegisterParticipant;

public sealed record RegisterParticipantCommand(Guid ActivityId, Guid UserId)
	: IRequest<ErrorOr<Success>>;
