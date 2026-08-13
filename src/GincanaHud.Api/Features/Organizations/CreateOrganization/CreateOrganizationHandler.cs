using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Organizations;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Organizations.CreateOrganization;

public sealed class CreateOrganizationHandler(AppDbContext db)
	: IRequestHandler<CreateOrganizationCommand, ErrorOr<OrganizationDto>>
{
	public async Task<ErrorOr<OrganizationDto>> Handle(
		CreateOrganizationCommand request,
		CancellationToken cancellationToken)
	{
		var org = Organization.Create(request.Name);
		if (org.IsError)
			return org.Errors;

		db.Organizations.Add(org.Value);
		await db.SaveChangesAsync(cancellationToken);
		return new OrganizationDto(org.Value.Id, org.Value.Name, org.Value.CreatedAt);
	}
}
