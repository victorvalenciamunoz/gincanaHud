using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.GetActivityByCode;

public sealed record GetActivityByCodeQuery(string JoinCode)
	: IRequest<ErrorOr<ActivitySummaryDto>>;
