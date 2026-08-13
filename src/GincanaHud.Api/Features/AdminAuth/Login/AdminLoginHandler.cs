using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.AdminAuth.Login;

public sealed class AdminLoginHandler(
	AppDbContext db,
	IPasswordHasher<AdminUser> passwordHasher)
	: IRequestHandler<AdminLoginCommand, ErrorOr<AdminLoginResponse>>
{
	public async Task<ErrorOr<AdminLoginResponse>> Handle(
		AdminLoginCommand request,
		CancellationToken cancellationToken)
	{
		var username = request.Username?.Trim() ?? "";
		var admin = await db.AdminUsers.AsNoTracking()
			.Include(a => a.Organization)
			.FirstOrDefaultAsync(a => a.Username == username, cancellationToken);

		if (admin is null || !admin.IsActive)
		{
			return Error.Validation(
				code: "AdminAuth.Invalid",
				description: "Usuario o contraseña incorrectos.");
		}

		var result = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password ?? "");
		if (result == PasswordVerificationResult.Failed)
		{
			return Error.Validation(
				code: "AdminAuth.Invalid",
				description: "Usuario o contraseña incorrectos.");
		}

		return new AdminLoginResponse(
			admin.Id,
			admin.Username,
			admin.Role.ToString(),
			admin.OrganizationId,
			admin.Organization?.Name);
	}
}
