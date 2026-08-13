namespace GincanaHud.App.Services;

public sealed class ApiOptions
{
	/// <summary>
	/// Base URL of the REST API. On a physical device use adb reverse:
	/// <c>adb reverse tcp:5263 tcp:5263</c> and keep <c>http://127.0.0.1:5263/</c>.
	/// Emulator: <c>http://10.0.2.2:5263/</c>.
	/// </summary>
	public string BaseUrl { get; set; } = "http://127.0.0.1:5263/";

	public string PlayerName { get; set; } = "jugador";
}
