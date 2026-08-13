namespace GincanaHud.App.Hud;

public sealed class HudFrame
{
	public double RelativeBearingDegrees { get; set; }
	public double DistanceMeters { get; set; } = double.NaN;
	public double PitchDegrees { get; set; }
	public bool IsInProximity { get; set; }
	public bool IsLocked { get; set; }
	public bool IsRunning { get; set; }
	public bool HasTarget { get; set; }
	public double Pulse { get; set; }
	public double CaptureFlash { get; set; }
	public string Status { get; set; } = "";
	public string TargetLabel { get; set; } = "";
	public string Clue { get; set; } = "";
}
