using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.JoinActivity;

public sealed record JoinActivityCommand(JoinActivityRequest Request)
	: IRequest<ErrorOr<JoinActivityResponse>>;
