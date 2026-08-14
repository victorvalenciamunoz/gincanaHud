using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GincanaHud.Api.Domain.Admin;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GincanaHud.Api;

public sealed class AdminJwtTokenService(IOptions<JwtOptions> options)
{
	public const string AccessTokenClaimType = "access_token";
	public const string OrganizationIdClaimType = "org_id";
	public const string OrganizationNameClaimType = "org_name";

	public (string Token, DateTimeOffset ExpiresAt) CreateToken(AdminUser admin, string? organizationName)
	{
		var opts = options.Value;
		if (!opts.IsConfigured)
			throw new InvalidOperationException("Jwt:SigningKey is not configured (min 32 characters).");

		var expires = DateTimeOffset.UtcNow.AddHours(Math.Clamp(opts.ExpirationHours, 1, 168));
		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
			new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
			new(ClaimTypes.Name, admin.Username),
			new(ClaimTypes.Role, admin.Role.ToString()),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
		};

		if (admin.Role == AdminRole.OrganizationAdmin && admin.OrganizationId is Guid orgId)
		{
			claims.Add(new Claim(OrganizationIdClaimType, orgId.ToString()));
			if (!string.IsNullOrWhiteSpace(organizationName))
				claims.Add(new Claim(OrganizationNameClaimType, organizationName));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var jwt = new JwtSecurityToken(
			issuer: opts.Issuer,
			audience: opts.Audience,
			claims: claims,
			notBefore: DateTime.UtcNow,
			expires: expires.UtcDateTime,
			signingCredentials: creds);

		return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
	}
}
