using GincanaHud.Api.Domain.Admin;

namespace GincanaHud.Api.Common.Http;

/// <summary>
/// Which organization a panel caller may see, from the token the middleware already validated (D24). Only for
/// functions with <c>[Authorize]</c>: anonymous ones have no user.
/// </summary>
public static class AdminScope
{
	/// <summary>
	/// <c>null</c> for the SuperAdmin (every organization). An organization admin without <c>org_id</c> gets
	/// <see cref="Guid.Empty"/>, which matches nothing.
	/// </summary>
	public static Guid? OrganizationOf(HttpRequest req)
	{
		var user = req.HttpContext.User;
		if (user.IsInRole(nameof(AdminRole.SuperAdmin)))
			return null;

		var claim = user.FindFirst(AdminJwtTokenService.OrganizationIdClaimType)?.Value;
		return Guid.TryParse(claim, out var organizationId) ? organizationId : Guid.Empty;
	}
}
