using System.Reflection;
using System.Security.Claims;
using GincanaHud.Api;
using GincanaHud.Api.Common.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Tests.Api;

public class FunctionAuthorizationTests
{
	// What the app and the login need without a session. Anything else asks for [Authorize] (D24).
	private static readonly string[] Anonymous =
	[
		"AdminLogin",
		"CapturePoi",
		"GetActivity",
		"GetActivityByCode",
		"GetRanking",
		"Health",
		"JoinActivity",
		"RegisterParticipant",
	];

	[Fact]
	public void Only_the_known_functions_are_anonymous()
	{
		var anonymous = typeof(JwtAuthorizationMiddleware).Assembly.GetTypes()
			.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Select(m => (Type: t, Method: m, Function: m.GetCustomAttribute<FunctionAttribute>())))
			.Where(x => x.Function is not null
				&& x.Method.GetCustomAttribute<AuthorizeAttribute>() is null
				&& x.Type.GetCustomAttribute<AuthorizeAttribute>() is null)
			.Select(x => x.Function!.Name)
			.Order()
			.ToArray();

		Assert.Equal(Anonymous, anonymous);
	}

	private static HttpRequest RequestAs(string role, string? organizationId = null)
	{
		var claims = new List<Claim> { new(ClaimTypes.Role, role) };
		if (organizationId is not null)
			claims.Add(new Claim(AdminJwtTokenService.OrganizationIdClaimType, organizationId));
		var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
		return http.Request;
	}

	[Fact]
	public void The_super_admin_sees_every_organization() =>
		Assert.Null(AdminScope.OrganizationOf(RequestAs("SuperAdmin")));

	[Fact]
	public void An_organization_admin_sees_its_organization()
	{
		var organizationId = Guid.NewGuid();

		Assert.Equal(organizationId, AdminScope.OrganizationOf(RequestAs("OrganizationAdmin", organizationId.ToString())));
	}

	[Fact]
	public void An_organization_admin_without_organization_sees_nothing() =>
		Assert.Equal(Guid.Empty, AdminScope.OrganizationOf(RequestAs("OrganizationAdmin")));

	[Fact]
	public void An_organization_admin_cannot_list_another_organization()
	{
		var own = Guid.NewGuid();

		Assert.Equal(own, AdminScope.Restrict(RequestAs("OrganizationAdmin", own.ToString()), requested: Guid.NewGuid()));
	}

	[Fact]
	public void The_super_admin_lists_what_it_asks_for()
	{
		var requested = Guid.NewGuid();

		Assert.Equal(requested, AdminScope.Restrict(RequestAs("SuperAdmin"), requested));
		Assert.Null(AdminScope.Restrict(RequestAs("SuperAdmin"), requested: null));
	}
}
