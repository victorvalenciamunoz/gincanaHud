using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GincanaHud.Api;

public static class JwtAuthExtensions
{
	public const string AdminPolicy = "Admin";
	public const string SuperAdminPolicy = "SuperAdmin";

	public static IServiceCollection AddGincanaJwtAuth(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
	{
		services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
		services.AddSingleton<AdminJwtTokenService>();

		var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
		if (!jwt.IsConfigured)
		{
			if (!env.IsDevelopment())
				throw new InvalidOperationException(
					"Jwt:SigningKey must be configured (min 32 characters) outside Development.");

			// Local-only fallback so `dotnet run` works before user-secrets.
			jwt.SigningKey = "GincanaHud-Dev-Only-Signing-Key-Change-Me!!";
			services.PostConfigure<JwtOptions>(o =>
			{
				if (!o.IsConfigured)
					o.SigningKey = jwt.SigningKey;
			});
		}

		services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = jwt.Issuer,
					ValidateAudience = true,
					ValidAudience = jwt.Audience,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
					NameClaimType = System.Security.Claims.ClaimTypes.Name,
					RoleClaimType = System.Security.Claims.ClaimTypes.Role,
					ClockSkew = TimeSpan.FromMinutes(1)
				};
			});

		services.AddAuthorization(options =>
		{
			options.AddPolicy(AdminPolicy, p => p.RequireRole("SuperAdmin", "OrganizationAdmin"));
			options.AddPolicy(SuperAdminPolicy, p => p.RequireRole("SuperAdmin"));
		});

		return services;
	}
}
