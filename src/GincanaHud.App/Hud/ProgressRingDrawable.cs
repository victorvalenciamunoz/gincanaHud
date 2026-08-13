namespace GincanaHud.App.Hud;

/// <summary>Anillo de progreso de la ruta (0–1).</summary>
public sealed class ProgressRingDrawable : IDrawable
{
	static readonly Color Track = Color.FromRgba(0x44, 0x55, 0x66, 0xAA);
	static readonly Color Fill = Color.FromRgb(0x7C, 0xFF, 0xB2);
	static readonly Color FillComplete = Color.FromRgb(0xFF, 0xC4, 0x2E);

	public double Progress { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var cx = dirtyRect.Center.X;
		var cy = dirtyRect.Center.Y;
		var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.5f - 3.5f;
		if (radius < 4)
			return;

		canvas.StrokeSize = 4.5f;
		canvas.StrokeColor = Track;
		canvas.DrawCircle(cx, cy, radius);

		var p = Math.Clamp(Progress, 0, 1);
		if (p < 0.005)
			return;

		canvas.StrokeColor = p >= 0.999 ? FillComplete : Fill;
		canvas.StrokeSize = 5f;
		// 12 en punto ( -90° ); sentido horario del progreso.
		var sweep = (float)(p * 360);
		canvas.DrawArc(
			cx - radius, cy - radius,
			radius * 2, radius * 2,
			-90, -90 + sweep,
			clockwise: false,
			closed: false);
	}
}
