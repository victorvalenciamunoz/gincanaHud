using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.AdminUsers.ListAdminUsers;

public sealed class ListAdminUsersHandler(AppDbContext db)
	: IRequestHandler<ListAdminUsersQuery, ErrorOr<IReadOnlyList<AdminUserDto>>>
{
	public async Task<ErrorOr<IReadOnlyList<AdminUserDto>>> Handle(
		ListAdminUsersQuery request,
		CancellationToken cancellationToken)
	{
		var items = await db.AdminUsers.AsNoTracking()
			.Include(a => a.Organization)
			.OrderBy(a => a.Username)
			.Select(a => new AdminUserDto(
				a.Id,
				a.Username,
				a.Role.ToString(),
				a.OrganizationId,
				a.Organization != null ? a.Organization.Name : null,
				a.IsActive,
				a.CreatedAt))
			.ToListAsync(cancellationToken);

		return items;
	}
}
