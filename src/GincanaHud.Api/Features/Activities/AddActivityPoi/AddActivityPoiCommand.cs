using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.AddActivityPoi;

public sealed record AddActivityPoiCommand(Guid ActivityId, CreatePoiRequest Request)
	: IRequest<ErrorOr<ActivityPoiDto>>;
