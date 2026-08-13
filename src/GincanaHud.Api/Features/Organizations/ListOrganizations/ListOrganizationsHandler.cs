using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Organizations.ListOrganizations;

public sealed class ListOrganizationsHandler(AppDbContext db)
	: IRequestHandler<ListOrganizationsQuery, ErrorOr<IReadOnlyList<OrganizationDto>>>
{
	public async Task<ErrorOr<IReadOnlyList<OrganizationDto>>> Handle(
		ListOrganizationsQuery request,
		CancellationToken cancellationToken)
	{
		var items = await db.Organizations.AsNoTracking()
			.OrderBy(o => o.Name)
			.Select(o => new OrganizationDto(o.Id, o.Name, o.CreatedAt))
			.ToListAsync(cancellationToken);
		return items;
	}
}
