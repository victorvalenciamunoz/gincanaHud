using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GincanaHud.Admin;

public static class AdminPrincipal
{
	public static bool IsSuperAdmin(ClaimsPrincipal? user) =>
		user?.IsInRole(AdminRoles.SuperAdmin) == true
		|| user?.HasClaim(ClaimTypes.Role, AdminRoles.SuperAdmin) == true;

	public static bool IsSuperAdmin(IHttpContextAccessor http) =>
		IsSuperAdmin(http.HttpContext?.User);
}
