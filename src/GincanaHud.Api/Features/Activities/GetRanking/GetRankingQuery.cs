using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Activities.GetRanking;

/// <param name="IncludeContacts">
/// The panel's view, with a session: players' email, phone and every id (D24). Without it, the app's view: no
/// contacts and only <paramref name="ViewerId"/>'s own id, because the id is what lets a phone rejoin (D25).
/// </param>
/// <param name="OrganizationId">When set, an activity of another organization is not found.</param>
public sealed record GetRankingQuery(
	Guid ActivityId,
	bool IncludeContacts = false,
	Guid? OrganizationId = null,
	Guid? ViewerId = null)
	: IRequest<ErrorOr<IReadOnlyList<RankingEntryDto>>>;
