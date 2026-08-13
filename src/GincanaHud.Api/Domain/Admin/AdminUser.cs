using ErrorOr;

namespace GincanaHud.Api.Domain.Admin;

public sealed class AdminUser
{
	private AdminUser() { }

	public Guid Id { get; private set; }
	public string Username { get; private set; } = "";
	public string PasswordHash { get; private set; } = "";
	public AdminRole Role { get; private set; }
	public Guid? OrganizationId { get; private set; }
	public Organizations.Organization? Organization { get; private set; }
	public bool IsActive { get; private set; } = true;
	public DateTimeOffset CreatedAt { get; private set; }

	public static ErrorOr<AdminUser> CreateSuperAdmin(string username, string passwordHash, Guid? id = null)
	{
		var user = CreateCore(username, passwordHash, AdminRole.SuperAdmin, organizationId: null, id);
		return user;
	}

	public static ErrorOr<AdminUser> CreateOrganizationAdmin(
		string username, string passwordHash, Guid organizationId, Guid? id = null)
	{
		if (organizationId == Guid.Empty)
			return Error.Validation(code: "AdminUser.Organization", description: "Organización requerida.");

		return CreateCore(username, passwordHash, AdminRole.OrganizationAdmin, organizationId, id);
	}

	private static ErrorOr<AdminUser> CreateCore(
		string? username,
		string passwordHash,
		AdminRole role,
		Guid? organizationId,
		Guid? id)
	{
		var name = username?.Trim() ?? "";
		if (name.Length is < 3 or > 100)
			return Error.Validation(code: "AdminUser.Username", description: "Usuario: 3–100 caracteres.");

		if (string.IsNullOrWhiteSpace(passwordHash))
			return Error.Validation(code: "AdminUser.Password", description: "Hash de contraseña requerido.");

		return new AdminUser
		{
			Id = id ?? Guid.NewGuid(),
			Username = name,
			PasswordHash = passwordHash,
			Role = role,
			OrganizationId = organizationId,
			IsActive = true,
			CreatedAt = DateTimeOffset.UtcNow
		};
	}

	public void ReplacePasswordHash(string passwordHash) => PasswordHash = passwordHash;

	public void PromoteToSuperAdmin()
	{
		Role = AdminRole.SuperAdmin;
		OrganizationId = null;
	}
}
