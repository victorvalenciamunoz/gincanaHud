namespace GincanaHud.App.Services;

public sealed class MauiCompassService : ICompassService
{
	private const double SmoothAlpha = 0.22;

	public event EventHandler<CompassReading>? HeadingChanged;
	public double? LastHeadingDegrees { get; private set; }
	public bool IsListening { get; private set; }

	public void Start()
	{
		if (IsListening)
			return;

		if (!Compass.Default.IsSupported)
			throw new InvalidOperationException("Brújula no disponible en este dispositivo.");

		Compass.Default.ReadingChanged += OnReadingChanged;
		Compass.Default.Start(SensorSpeed.UI);
		IsListening = true;
	}

	public void Stop()
	{
		if (!IsListening)
			return;

		Compass.Default.ReadingChanged -= OnReadingChanged;
		if (Compass.Default.IsMonitoring)
			Compass.Default.Stop();

		IsListening = false;
		LastHeadingDegrees = null;
	}

	private void OnReadingChanged(object? sender, CompassChangedEventArgs e)
	{
		var raw = e.Reading.HeadingMagneticNorth;
		var smoothed = LastHeadingDegrees is double prev
			? SmoothAngleDegrees(prev, raw, SmoothAlpha)
			: raw;

		LastHeadingDegrees = smoothed;
		HeadingChanged?.Invoke(this, new CompassReading(smoothed, DateTimeOffset.UtcNow));
	}

	private static double SmoothAngleDegrees(double from, double to, double alpha)
	{
		var delta = ((to - from + 540) % 360) - 180;
		var next = from + delta * alpha;
		return (next % 360 + 360) % 360;
	}
}
