using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.GetRanking;

public sealed record GetRankingQuery(Guid ActivityId)
	: IRequest<ErrorOr<IReadOnlyList<RankingEntryDto>>>;
