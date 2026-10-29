namespace GincanaHud.Shared;

public static class ApiHeaders
{
	/// <summary>
	/// The panel's JWT. Static Web Apps keeps <c>Authorization</c> for its own auth and does not pass it on to
	/// managed functions (D22), so the panel sends the token here; the Api still accepts <c>Authorization</c> too.
	/// </summary>
	public const string PanelToken = "X-Gincana-Token";
}
