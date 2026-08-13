using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.GetActivity;

public sealed record GetActivityQuery(Guid Id, Guid? UserId)
	: IRequest<ErrorOr<ActivityDetailDto>>;
