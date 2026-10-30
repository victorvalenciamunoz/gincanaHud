using ErrorOr;

using GincanaHud.Api.Common.Messaging;

using GincanaHud.Shared;



namespace GincanaHud.Api.Features.Activities.GetLiveProgress;



public sealed record GetLiveProgressQuery(Guid ActivityId, Guid? OrganizationScope = null)

	: IRequest<ErrorOr<LiveProgressDto>>;

