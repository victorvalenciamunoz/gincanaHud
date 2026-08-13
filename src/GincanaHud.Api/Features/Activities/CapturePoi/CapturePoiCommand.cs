using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.CapturePoi;

public sealed record CapturePoiCommand(Guid ActivityId, CaptureRequest Request)
	: IRequest<ErrorOr<CaptureResponse>>;
