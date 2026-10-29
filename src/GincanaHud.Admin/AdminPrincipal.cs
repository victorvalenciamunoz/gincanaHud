using System.Security.Claims;

namespace GincanaHud.Admin;

public static class AdminPrincipal
{
	public static bool IsSuperAdmin(ClaimsPrincipal? user) =>
		user?.IsInRole(AdminRoles.SuperAdmin) == true
		|| user?.HasClaim(ClaimTypes.Role, AdminRoles.SuperAdmin) == true;
}
