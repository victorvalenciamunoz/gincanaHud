namespace GincanaHud.Shared;

public readonly record struct ArScreenPoint(
	double X,
	double Y,
	bool InView,
	double Scale,
	double NormalizedX,
	double NormalizedY);

/// <summary>Proyección pinhole aproximada de un punto GPS al plano de la cámara.</summary>
public static class ArProjection
{
	public const double DefaultHorizontalFovDegrees = 62;
	public const double CameraHeightMeters = 1.55;
	/// <summary>Compensa el sesgo del pitch (el marcador tendía a subir demasiado).</summary>
	public const double PitchBiasDegrees = 10;
	/// <summary>Margen superior para no tapar el panel de pista del HUD.</summary>
	public const double DefaultTopSafeFraction = 0.28;

	public static ArScreenPoint Project(
		double relativeBearingDegrees,
		double distanceMeters,
		double pitchDegrees,
		double viewWidth,
		double viewHeight,
		double horizontalFovDegrees = DefaultHorizontalFovDegrees,
		double topSafeFraction = DefaultTopSafeFraction)
	{
		var aspect = viewHeight <= 0 ? 1.5 : viewWidth / viewHeight;
		var hFovRad = Deg2Rad(horizontalFovDegrees);
		var vFovRad = 2 * Math.Atan(Math.Tan(hFovRad / 2) / Math.Max(aspect, 0.5));
		var vFovDeg = Rad2Deg(vFovRad);

		var dist = Math.Max(distanceMeters, 0.4);
		// Objetivo a ras de suelo: ángulo de elevación negativo (bajo el horizonte).
		var elevationDeg = Rad2Deg(Math.Atan2(-CameraHeightMeters, dist));
		// pitch: 0 = mirando horizonte; + = mirar arriba; − = mirar abajo.
		var relativeElevation = elevationDeg - (pitchDegrees + PitchBiasDegrees);

		var nx = relativeBearingDegrees / (horizontalFovDegrees * 0.5);
		var ny = relativeElevation / (vFovDeg * 0.5);

		const double soft = 1.2;
		var inView = Math.Abs(nx) <= soft && Math.Abs(ny) <= soft;

		var clampedX = Math.Clamp(nx, -1.35, 1.35);
		var clampedY = Math.Clamp(ny, -1.35, 1.35);
		var x = viewWidth * 0.5 * (1 + clampedX);
		var y = viewHeight * 0.5 * (1 - clampedY);

		// No pintar bajo el panel superior (pista / progreso).
		var topSafe = viewHeight * Math.Clamp(topSafeFraction, 0.15, 0.4);
		var bottomSafe = viewHeight * 0.82;
		y = Math.Clamp(y, topSafe, bottomSafe);

		// Más cerca → más grande.
		var scale = Math.Clamp(2.35 - Math.Log10(dist + 1) * 0.95, 0.42, 2.6);

		return new ArScreenPoint(x, y, inView, scale, nx, ny);
	}

	static double Deg2Rad(double d) => d * Math.PI / 180;
	static double Rad2Deg(double r) => r * 180 / Math.PI;
}
