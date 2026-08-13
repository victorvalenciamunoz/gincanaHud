namespace GincanaHud.App.Services;

public interface ICaptureFxService
{
	/// <summary>Feedback visual ya lo anima el HUD; esto dispara campanas + háptica.</summary>
	void PlayCaptureSuccess();

	/// <summary>Pulso háptico al acercarse (&lt;15 m). Más frecuente cuanto más cerca.</summary>
	void TickProximity(double distanceMeters);
}
