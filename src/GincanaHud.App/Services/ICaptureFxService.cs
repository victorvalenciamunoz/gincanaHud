namespace GincanaHud.App.Services;

public interface ICaptureFxService
{
	/// <summary>Feedback visual ya lo anima el HUD; esto dispara campanas + háptica.</summary>
	void PlayCaptureSuccess();

	/// <summary>Una vibración corta al entrar a 15 m del POI.</summary>
	void NotifyProximity();
}
