using ErrorOr;
using GincanaHud.Api.Common.Messaging;

namespace GincanaHud.Api.Features.Activities.RemoveActivityPoi;

public sealed record RemoveActivityPoiCommand(Guid ActivityId, Guid PoiId)
	: IRequest<ErrorOr<Deleted>>;
