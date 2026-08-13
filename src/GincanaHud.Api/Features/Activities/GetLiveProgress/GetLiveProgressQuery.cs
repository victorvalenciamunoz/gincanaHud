using ErrorOr;

using GincanaHud.Api.Common.Messaging;

using GincanaHud.Shared;



namespace GincanaHud.Api.Features.Activities.GetLiveProgress;



public sealed record GetLiveProgressQuery(Guid ActivityId)

	: IRequest<ErrorOr<LiveProgressDto>>;

