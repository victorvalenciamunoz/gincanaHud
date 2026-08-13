namespace GincanaHud.Api;

public sealed class AdminBootstrapOptions
{
	public const string SectionName = "AdminBootstrap";

	public string Username { get; set; } = "";
	public string Password { get; set; } = "";

	public bool IsConfigured =>
		!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
