namespace GincanaHud.App.Services;

public sealed record OrientationReading(double PitchDegrees, DateTimeOffset Timestamp);

public interface IOrientationService
{
	event EventHandler<OrientationReading>? PitchChanged;
	double? LastPitchDegrees { get; }
	bool IsListening { get; }
	void Start();
	void Stop();
}

/// <summary>
/// Pitch aproximado desde acelerómetro (retrato): 0 ≈ horizonte, − mirar abajo, + mirar arriba.
/// Con filtro exponencial para reducir temblor del HUD.
/// </summary>
public sealed class MauiOrientationService : IOrientationService
{
	private const double SmoothAlpha = 0.18;

	public event EventHandler<OrientationReading>? PitchChanged;
	public double? LastPitchDegrees { get; private set; }
	public bool IsListening { get; private set; }

	public void Start()
	{
		if (IsListening)
			return;

		if (!Accelerometer.Default.IsSupported)
			return;

		Accelerometer.Default.ReadingChanged += OnReadingChanged;
		Accelerometer.Default.Start(SensorSpeed.UI);
		IsListening = true;
	}

	public void Stop()
	{
		if (!IsListening)
			return;

		Accelerometer.Default.ReadingChanged -= OnReadingChanged;
		if (Accelerometer.Default.IsMonitoring)
			Accelerometer.Default.Stop();

		IsListening = false;
		LastPitchDegrees = null;
	}

	private void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
	{
		var a = e.Reading.Acceleration;
		// Retrato: Y ≈ 1 al vertical; Z sale hacia el usuario.
		var raw = Math.Atan2(-a.Z, Math.Max(0.05, a.Y)) * (180.0 / Math.PI);
		raw = Math.Clamp(raw, -75, 75);

		var smoothed = LastPitchDegrees is double prev
			? prev + (raw - prev) * SmoothAlpha
			: raw;

		LastPitchDegrees = smoothed;
		PitchChanged?.Invoke(this, new OrientationReading(smoothed, DateTimeOffset.UtcNow));
	}
}
