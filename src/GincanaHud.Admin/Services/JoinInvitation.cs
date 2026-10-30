namespace GincanaHud.Admin.Services;

public static class JoinInvitation
{
	/// <summary>Short text to paste in WhatsApp or an email (VISION: code and QR for posters, email or WhatsApp).</summary>
	public static string Text(string title, string joinCode) =>
		$"«{title.Trim()}»: abre la app GincanaHud y únete con el código {joinCode.Trim().ToUpperInvariant()}, " +
		"o escanea el QR del cartel.";
}
