using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Users.ListUsers;

public sealed class ListUsersHandler(AppDbContext db)
	: IRequestHandler<ListUsersQuery, ErrorOr<IReadOnlyList<PlayerDto>>>
{
	public async Task<ErrorOr<IReadOnlyList<PlayerDto>>> Handle(
		ListUsersQuery request,
		CancellationToken cancellationToken)
	{
		var rows = await (
			from p in db.ActivityParticipants.AsNoTracking()
			join a in db.Activities.AsNoTracking() on p.ActivityId equals a.Id
			join u in db.Users.AsNoTracking() on p.UserId equals u.Id
			where request.OrganizationId == null || a.OrganizationId == request.OrganizationId
			orderby u.DisplayName, p.JoinedAt
			select new
			{
				UserId = u.Id,
				u.DisplayName,
				u.ContactEmail,
				u.ContactPhone,
				u.CreatedAt,
				ActivityId = a.Id,
				ActivityTitle = a.Title,
				p.JoinedAt
			}).ToListAsync(cancellationToken);

		var players = rows
			.GroupBy(r => r.UserId)
			.Select(g =>
			{
				var first = g.First();
				return new PlayerDto(
					first.UserId,
					first.DisplayName,
					first.ContactEmail,
					first.ContactPhone,
					first.CreatedAt,
					g.Select(x => new PlayerActivityDto(x.ActivityId, x.ActivityTitle, x.JoinedAt))
						.OrderByDescending(a => a.JoinedAt)
						.ToList());
			})
			.OrderBy(p => p.DisplayName)
			.ToList();

		// SuperAdmin (sin filtro): incluir usuarios sin ninguna participación.
		if (request.OrganizationId is null)
		{
			var withPart = players.Select(p => p.Id).ToHashSet();
			var orphans = await db.Users.AsNoTracking()
				.Where(u => !withPart.Contains(u.Id))
				.OrderBy(u => u.DisplayName)
				.Select(u => new PlayerDto(
					u.Id, u.DisplayName, u.ContactEmail, u.ContactPhone, u.CreatedAt,
					Array.Empty<PlayerActivityDto>()))
				.ToListAsync(cancellationToken);
			players = players.Concat(orphans).OrderBy(p => p.DisplayName).ToList();
		}

		return players;
	}
}
