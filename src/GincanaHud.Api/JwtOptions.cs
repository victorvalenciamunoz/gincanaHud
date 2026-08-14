namespace GincanaHud.Api;

public sealed class JwtOptions
{
	public const string SectionName = "Jwt";

	/// <summary>HS256 signing key (min ~32 chars). Required outside Development.</summary>
	public string SigningKey { get; set; } = "";

	public string Issuer { get; set; } = "GincanaHud.Api";
	public string Audience { get; set; } = "GincanaHud.Admin";
	public int ExpirationHours { get; set; } = 12;

	public bool IsConfigured => !string.IsNullOrWhiteSpace(SigningKey) && SigningKey.Length >= 32;
}
