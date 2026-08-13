using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.AdminUsers.CreateOrgAdmin;

public sealed class CreateOrgAdminHandler(
	AppDbContext db,
	IPasswordHasher<AdminUser> passwordHasher)
	: IRequestHandler<CreateOrgAdminCommand, ErrorOr<AdminUserDto>>
{
	public async Task<ErrorOr<AdminUserDto>> Handle(
		CreateOrgAdminCommand request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
		{
			return Error.Validation(
				code: "AdminUser.Password",
				description: "La contraseña debe tener al menos 6 caracteres.");
		}

		var org = await db.Organizations.AsNoTracking()
			.FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);
		if (org is null)
			return Error.NotFound(code: "Organization.NotFound", description: "Organización no encontrada.");

		var username = request.Username?.Trim() ?? "";
		var taken = await db.AdminUsers.AnyAsync(a => a.Username == username, cancellationToken);
		if (taken)
			return Error.Conflict(code: "AdminUser.UsernameTaken", description: "Ese usuario ya existe.");

		var draft = AdminUser.CreateOrganizationAdmin(username, "pending", request.OrganizationId);
		if (draft.IsError)
			return draft.Errors;

		var hash = passwordHasher.HashPassword(draft.Value, request.Password);
		draft.Value.ReplacePasswordHash(hash);

		db.AdminUsers.Add(draft.Value);
		await db.SaveChangesAsync(cancellationToken);

		return new AdminUserDto(
			draft.Value.Id,
			draft.Value.Username,
			draft.Value.Role.ToString(),
			draft.Value.OrganizationId,
			org.Name,
			draft.Value.IsActive,
			draft.Value.CreatedAt);
	}
}
