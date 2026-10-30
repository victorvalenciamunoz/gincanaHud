using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.GetRanking;

/// <param name="IncludeContacts">Players' email and phone: only for the panel, with a session (D24).</param>
/// <param name="OrganizationId">When set, an activity of another organization is not found.</param>
public sealed record GetRankingQuery(Guid ActivityId, bool IncludeContacts = false, Guid? OrganizationId = null)
	: IRequest<ErrorOr<IReadOnlyList<RankingEntryDto>>>;
