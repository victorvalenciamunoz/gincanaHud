using GincanaHud.Shared;

namespace GincanaHud.App.Hud;

/// <summary>HUD AR 2D: marcador del POI proyectado sobre la cámara + brújula mini.</summary>
public sealed class HudDrawable : IDrawable
{
	static readonly Color Mint = Color.FromRgb(0x7C, 0xFF, 0xB2);
	static readonly Color Cyan = Color.FromRgb(0x4D, 0xD2, 0xFF);
	static readonly Color Amber = Color.FromRgb(0xFF, 0xC4, 0x2E);
	static readonly Color Alert = Color.FromRgb(0xFF, 0x5C, 0x5C);

	public HudFrame Frame { get; set; } = new();

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.SaveState();

		DrawLightVignette(canvas, dirtyRect);

		if (Frame.IsRunning && Frame.HasTarget && !double.IsNaN(Frame.DistanceMeters))
		{
			var pt = ArProjection.Project(
				Frame.RelativeBearingDegrees,
				Frame.DistanceMeters,
				Frame.PitchDegrees,
				dirtyRect.Width,
				dirtyRect.Height);

			if (pt.InView)
				DrawWorldMarker(canvas, dirtyRect, (float)pt.X, (float)pt.Y, (float)pt.Scale);
			else
				DrawEdgeCue(canvas, dirtyRect, (float)pt.NormalizedX, (float)pt.NormalizedY);

			DrawAimReticle(canvas, dirtyRect);
			DrawDirectionArc(canvas, dirtyRect);
		}
		else if (Frame.IsRunning)
		{
			DrawAimReticle(canvas, dirtyRect);
			DrawDirectionArc(canvas, dirtyRect);
			DrawIdleHint(canvas, dirtyRect);
			DrawBottomStatus(canvas, dirtyRect);
		}
		else
		{
			DrawIdleHint(canvas, dirtyRect);
			DrawBottomStatus(canvas, dirtyRect);
		}

		if (Frame.IsLocked)
			DrawLockWash(canvas, dirtyRect);

		if (Frame.CaptureFlash > 0.01)
			DrawCaptureFlash(canvas, dirtyRect);

		canvas.RestoreState();
	}

	private static void DrawLightVignette(ICanvas canvas, RectF r)
	{
		var band = Math.Min(r.Width, r.Height) * 0.12f;
		canvas.FillColor = Color.FromRgba(0f, 0f, 0f, 0.28f);
		canvas.FillRectangle(r.Left, r.Top, r.Width, band * 0.85f);
		canvas.FillRectangle(r.Left, r.Bottom - band * 1.1f, r.Width, band * 1.1f);
	}

	private void DrawIdleHint(ICanvas canvas, RectF r)
	{
		var cx = r.Center.X;
		var cy = r.Center.Y;
		canvas.StrokeSize = 1.5f;
		canvas.StrokeColor = Color.FromRgba(Mint.Red, Mint.Green, Mint.Blue, 0.4f);
		canvas.DrawCircle(cx, cy, 28);
		canvas.FontSize = 13;
		canvas.FontColor = Color.FromRgba(1f, 1f, 1f, 0.7f);
		canvas.DrawString(
			Frame.IsRunning ? "Sin objetivo — Unirse / POIs" : "Pulsa INICIAR",
			cx - 120, cy + 40, 240, 24,
			HorizontalAlignment.Center, VerticalAlignment.Top);
	}

	private void DrawWorldMarker(ICanvas canvas, RectF view, float x, float y, float scale)
	{
		var pulse = (float)Frame.Pulse;
		var baseSize = 22f * scale;
		var glow = Frame.IsLocked ? Mint : (Frame.IsInProximity ? Amber : Cyan);
		var alphaPulse = Frame.IsInProximity || Frame.IsLocked
			? 0.35f + 0.45f * (1f - pulse)
			: 0.55f;

		var groundY = Math.Min(view.Bottom - 150, y + baseSize * 2.8f);
		canvas.StrokeSize = 2;
		canvas.StrokeColor = Color.FromRgba(glow.Red, glow.Green, glow.Blue, 0.35f);
		canvas.DrawLine(x, y + baseSize * 0.6f, x, groundY);
		canvas.StrokeSize = 1.5f;
		canvas.DrawEllipse(x - 10 * scale, groundY - 4, 20 * scale, 8);

		canvas.StrokeSize = 3 + 6 * (Frame.IsLocked ? (1 - pulse) : 0.3f);
		canvas.StrokeColor = Color.FromRgba(glow.Red, glow.Green, glow.Blue, alphaPulse * 0.55f);
		canvas.DrawCircle(x, y, baseSize * (1.15f + 0.25f * pulse));

		var d = baseSize;
		var path = new PathF();
		path.MoveTo(x, y - d);
		path.LineTo(x + d * 0.72f, y);
		path.LineTo(x, y + d);
		path.LineTo(x - d * 0.72f, y);
		path.Close();

		canvas.FillColor = Color.FromRgba(glow.Red, glow.Green, glow.Blue, Frame.IsLocked ? 0.55f : 0.28f);
		canvas.FillPath(path);
		canvas.StrokeSize = Frame.IsLocked ? 3f : 2f;
		canvas.StrokeColor = glow;
		canvas.DrawPath(path);

		canvas.FillColor = Colors.White;
		canvas.FillCircle(x, y, Math.Max(3f, 4f * scale));

		var b = d * 1.35f;
		var len = d * 0.55f;
		canvas.StrokeSize = 2;
		canvas.StrokeColor = Color.FromRgba(1f, 1f, 1f, 0.75f);
		DrawBracketCorners(canvas, x, y, b, len);

		var label = "OBJETIVO";
		var dist = Frame.DistanceMeters >= 1000
			? $"{Frame.DistanceMeters / 1000:0.0} km"
			: $"{Frame.DistanceMeters:0} m";
		var boxW = Math.Min(view.Width - 24, 160f);
		var boxH = 40f;
		var bx = Math.Clamp(x - boxW / 2, 12, view.Width - boxW - 12);
		// Etiqueta debajo del marcador para no chocar con la pista superior.
		var by = Math.Clamp(y + d + 8, topSafeMin(view), view.Height - 170);

		canvas.FillColor = Color.FromRgba(0.04f, 0.07f, 0.1f, 0.92f);
		canvas.FillRoundedRectangle(bx, by, boxW, boxH, 8);
		canvas.StrokeSize = 1.5f;
		canvas.StrokeColor = Color.FromRgba(glow.Red, glow.Green, glow.Blue, 0.9f);
		canvas.DrawRoundedRectangle(bx, by, boxW, boxH, 8);

		canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetchBold");
		canvas.FontSize = 11;
		canvas.FontColor = Colors.White;
		canvas.DrawString(label, bx + 8, by + 3, boxW - 16, 16,
			HorizontalAlignment.Center, VerticalAlignment.Top);
		canvas.FontSize = 16;
		canvas.FontColor = Colors.White;
		canvas.DrawString(dist, bx + 8, by + 18, boxW - 16, 20,
			HorizontalAlignment.Center, VerticalAlignment.Top);

		if (Frame.IsLocked)
		{
			canvas.FontSize = 12;
			canvas.FontColor = Mint;
			canvas.DrawString("LOCK", x - 30, y + d + boxH + 12, 60, 18,
				HorizontalAlignment.Center, VerticalAlignment.Top);
		}
	}

	private static float topSafeMin(RectF view) => Math.Max(120f, view.Height * 0.22f);

	private static void DrawBracketCorners(ICanvas canvas, float x, float y, float half, float len)
	{
		canvas.DrawLine(x - half, y - half, x - half + len, y - half);
		canvas.DrawLine(x - half, y - half, x - half, y - half + len);
		canvas.DrawLine(x + half, y - half, x + half - len, y - half);
		canvas.DrawLine(x + half, y - half, x + half, y - half + len);
		canvas.DrawLine(x - half, y + half, x - half + len, y + half);
		canvas.DrawLine(x - half, y + half, x - half, y + half - len);
		canvas.DrawLine(x + half, y + half, x + half - len, y + half);
		canvas.DrawLine(x + half, y + half, x + half, y + half - len);
	}

	private void DrawEdgeCue(ICanvas canvas, RectF view, float nx, float ny)
	{
		var margin = 36f;
		var sx = Math.Clamp(view.Center.X + nx * (view.Width * 0.5f - margin), margin, view.Width - margin);
		var sy = Math.Clamp(view.Center.Y - ny * (view.Height * 0.5f - margin - 40), topSafeMin(view), view.Height - 160);
		var ang = MathF.Atan2(nx, Math.Max(0.01f, -ny + 0.001f));

		canvas.FillColor = Color.FromRgba(Alert.Red, Alert.Green, Alert.Blue, 0.9f);
		var tip = 18f;
		var path = new PathF();
		path.MoveTo(sx + tip * MathF.Sin(ang), sy - tip * MathF.Cos(ang));
		path.LineTo(sx - 10 * MathF.Cos(ang), sy - 10 * MathF.Sin(ang));
		path.LineTo(sx + 10 * MathF.Cos(ang), sy + 10 * MathF.Sin(ang));
		path.Close();
		canvas.FillPath(path);

		canvas.FontSize = 12;
		canvas.FontColor = Colors.White;
		var dist = double.IsNaN(Frame.DistanceMeters) ? "" : $"{Frame.DistanceMeters:0} m";
		var hint = FormatEdgeHint(Frame.RelativeBearingDegrees);
		canvas.DrawString(hint, sx - 48, sy + 14, 96, 16,
			HorizontalAlignment.Center, VerticalAlignment.Top);
		if (dist.Length > 0)
		{
			canvas.FontSize = 11;
			canvas.FontColor = Color.FromRgba(1f, 1f, 1f, 0.85f);
			canvas.DrawString(dist, sx - 36, sy + 30, 72, 16,
				HorizontalAlignment.Center, VerticalAlignment.Top);
		}
	}

	private static string FormatEdgeHint(double relativeBearingDegrees)
	{
		var abs = Math.Abs(relativeBearingDegrees);
		if (abs <= 15)
			return "Delante";
		return relativeBearingDegrees >= 0 ? "→ Derecha" : "← Izquierda";
	}

	private void DrawAimReticle(ICanvas canvas, RectF view)
	{
		var cx = view.Center.X;
		var cy = view.Center.Y;
		var arm = 18f;
		var gap = 6f;
		var col = Frame.IsLocked ? Mint : Color.FromRgba(1f, 1f, 1f, 0.55f);
		canvas.StrokeSize = Frame.IsLocked ? 2.4f : 1.4f;
		canvas.StrokeColor = col;
		canvas.DrawLine(cx - arm, cy, cx - gap, cy);
		canvas.DrawLine(cx + gap, cy, cx + arm, cy);
		canvas.DrawLine(cx, cy - arm, cx, cy - gap);
		canvas.DrawLine(cx, cy + gap, cx, cy + arm);
		canvas.DrawCircle(cx, cy, 3);
	}

	private void DrawDirectionArc(ICanvas canvas, RectF view)
	{
		var cx = view.Center.X;
		var plateH = 72f;
		var plateBottom = view.Bottom - 70f;
		var plateTop = plateBottom - plateH;
		var plateLeft = 14f;
		var plateW = view.Width - 28f;

		canvas.FillColor = Color.FromRgba(0.04f, 0.06f, 0.09f, 0.88f);
		canvas.FillRoundedRectangle(plateLeft, plateTop, plateW, plateH, 12);
		canvas.StrokeSize = 1.4f;
		canvas.StrokeColor = Frame.IsLocked
			? Color.FromRgba(Mint.Red, Mint.Green, Mint.Blue, 0.65f)
			: Color.FromRgba(Cyan.Red, Cyan.Green, Cyan.Blue, 0.4f);
		canvas.DrawRoundedRectangle(plateLeft, plateTop, plateW, plateH, 12);

		var arcCy = plateTop + 38f;
		var radius = Math.Min(plateW * 0.42f, 150f);
		var flatten = 0.52f;

		// Arco de fondo (−90° … +90°).
		var arc = new PathF();
		const int steps = 40;
		for (var i = 0; i <= steps; i++)
		{
			var t = i / (float)steps;
			var deg = -90f + 180f * t;
			var rad = deg * MathF.PI / 180f;
			var x = cx + radius * MathF.Sin(rad);
			var y = arcCy - radius * MathF.Cos(rad) * flatten;
			if (i == 0)
				arc.MoveTo(x, y);
			else
				arc.LineTo(x, y);
		}

		canvas.StrokeSize = 3.2f;
		canvas.StrokeColor = Color.FromRgba(Cyan.Red, Cyan.Green, Cyan.Blue, 0.35f);
		canvas.DrawPath(arc);

		// Marcas: izquierda / centro / derecha.
		DrawArcTick(canvas, cx, arcCy, radius, flatten, -60f, 7f);
		DrawArcTick(canvas, cx, arcCy, radius, flatten, -30f, 5f);
		DrawArcTick(canvas, cx, arcCy, radius, flatten, 0f, 11f, Colors.White);
		DrawArcTick(canvas, cx, arcCy, radius, flatten, 30f, 5f);
		DrawArcTick(canvas, cx, arcCy, radius, flatten, 60f, 7f);

		canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetch");
		canvas.FontSize = 10;
		canvas.FontColor = Color.FromRgba(1f, 1f, 1f, 0.55f);
		canvas.DrawString("IZQ", cx - radius + 4, plateTop + 6, 36, 14,
			HorizontalAlignment.Left, VerticalAlignment.Top);
		canvas.DrawString("DER", cx + radius - 40, plateTop + 6, 36, 14,
			HorizontalAlignment.Right, VerticalAlignment.Top);

		var hasBearing = Frame.HasTarget && !double.IsNaN(Frame.DistanceMeters);
		if (hasBearing)
		{
			// ±180 → pin en el borde del arco si está detrás.
			var display = (float)Math.Clamp(Frame.RelativeBearingDegrees, -120, 120);
			var rad = display * MathF.PI / 180f;
			var mx = cx + radius * MathF.Sin(rad);
			var my = arcCy - radius * MathF.Cos(rad) * flatten;
			var aligned = Math.Abs(Frame.RelativeBearingDegrees) <= 15;
			var needle = Frame.IsLocked
				? Mint
				: Frame.IsInProximity
					? Amber
					: aligned ? Mint : Alert;

			// Triángulo apuntando al arco.
			var tip = 9f;
			var path = new PathF();
			path.MoveTo(mx, my - tip * 0.2f);
			path.LineTo(mx - 7f, my + tip);
			path.LineTo(mx + 7f, my + tip);
			path.Close();
			canvas.FillColor = needle;
			canvas.FillPath(path);
			canvas.FillColor = Colors.White;
			canvas.FillCircle(mx, my - 1f, 2.4f);

			var dist = Frame.DistanceMeters >= 1000
				? $"{Frame.DistanceMeters / 1000:0.0} km"
				: $"{Frame.DistanceMeters:0} m";
			canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetchBold");
			canvas.FontSize = 15;
			canvas.FontColor = Colors.White;
			canvas.DrawString(dist, cx - 50, plateBottom - 26, 100, 20,
				HorizontalAlignment.Center, VerticalAlignment.Center);

			var hint = FormatArcHint(Frame.RelativeBearingDegrees);
			canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetch");
			canvas.FontSize = 11;
			canvas.FontColor = Color.FromRgba(needle.Red, needle.Green, needle.Blue, 0.95f);
			canvas.DrawString(hint, cx - 80, plateTop + 4, 160, 16,
				HorizontalAlignment.Center, VerticalAlignment.Top);
		}
		else if (!string.IsNullOrWhiteSpace(Frame.Status))
		{
			canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetch");
			canvas.FontSize = 12;
			canvas.FontColor = Colors.White;
			canvas.DrawString(Frame.Status, plateLeft + 12, plateTop + 22, plateW - 24, 28,
				HorizontalAlignment.Center, VerticalAlignment.Center);
		}
	}

	private static void DrawArcTick(
		ICanvas canvas,
		float cx,
		float cy,
		float radius,
		float flatten,
		float degrees,
		float length,
		Color? color = null)
	{
		var rad = degrees * MathF.PI / 180f;
		var ux = MathF.Sin(rad);
		var uy = -MathF.Cos(rad) * flatten;
		// Normalizar dirección radial aproximada.
		var len = MathF.Sqrt(ux * ux + uy * uy);
		if (len < 0.001f)
			return;
		ux /= len;
		uy /= len;
		var x = cx + radius * MathF.Sin(rad);
		var y = cy - radius * MathF.Cos(rad) * flatten;
		canvas.StrokeSize = degrees == 0 ? 2.4f : 1.6f;
		canvas.StrokeColor = color ?? Color.FromRgba(1f, 1f, 1f, 0.45f);
		canvas.DrawLine(x, y, x + ux * length, y + uy * length);
	}

	private static string FormatArcHint(double relativeBearingDegrees)
	{
		var abs = Math.Abs(relativeBearingDegrees);
		if (abs <= 15)
			return "DE FRENTE";
		if (abs <= 45)
			return relativeBearingDegrees > 0 ? "UN POCO →" : "← UN POCO";
		if (abs <= 120)
			return relativeBearingDegrees > 0 ? "GIRA →" : "← GIRA";
		return relativeBearingDegrees > 0 ? "DATE LA VUELTA →" : "← DATE LA VUELTA";
	}

	private static void DrawLockWash(ICanvas canvas, RectF r)
	{
		canvas.StrokeSize = 3;
		canvas.StrokeColor = Color.FromRgba(Mint.Red, Mint.Green, Mint.Blue, 0.5f);
		canvas.DrawRectangle(r.Left + 6, r.Top + 6, r.Width - 12, r.Height - 12);
	}

	private void DrawCaptureFlash(ICanvas canvas, RectF r)
	{
		// CaptureFlash: 1 → 0. Edad de la explosión.
		var life = (float)Math.Clamp(Frame.CaptureFlash, 0, 1);
		var age = 1f - life;
		var cx = r.Center.X;
		var cy = r.Center.Y * 0.92f;

		// Destello central.
		canvas.FillColor = Color.FromRgba(1f, 1f, 0.85f, 0.22f * life);
		canvas.FillCircle(cx, cy, 28f + 90f * age);
		canvas.FillColor = Color.FromRgba(Mint.Red, Mint.Green, Mint.Blue, 0.18f * life);
		canvas.FillRectangle(r);

		const int starCount = 28;
		var maxR = Math.Min(r.Width, r.Height) * 0.48f;
		for (var i = 0; i < starCount; i++)
		{
			// Distribución estable (sin Random por frame).
			var seed = i * 0.6180339f;
			var angle = seed * MathF.Tau + age * (0.35f + (i % 5) * 0.08f);
			var speed = 0.45f + (i % 7) * 0.08f;
			var ease = 1f - (1f - age) * (1f - age); // ease-out
			var dist = maxR * speed * ease;
			var x = cx + MathF.Cos(angle) * dist;
			var y = cy + MathF.Sin(angle) * dist * 0.92f;
			var size = (10f + (i % 4) * 3.5f) * life * (0.65f + speed * 0.25f);
			var twinkle = 0.55f + 0.45f * MathF.Sin(age * 18f + i);
			var col = (i % 3) switch
			{
				0 => Amber,
				1 => Mint,
				_ => Colors.White,
			};
			canvas.FillColor = Color.FromRgba(col.Red, col.Green, col.Blue, Math.Clamp(life * twinkle, 0, 1));
			DrawStar(canvas, x, y, size, points: 4, rotation: angle + age * 4f);
		}

		if (life > 0.25f)
		{
			var textA = Math.Clamp((life - 0.25f) / 0.5f, 0, 1);
			canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetchBold");
			canvas.FontSize = 22;
			canvas.FontColor = Color.FromRgba(1f, 1f, 1f, textA);
			canvas.DrawString("¡CAPTURADO!", cx - 110, cy - 14, 220, 28,
				HorizontalAlignment.Center, VerticalAlignment.Center);
		}
	}

	private static void DrawStar(ICanvas canvas, float cx, float cy, float radius, int points, float rotation)
	{
		if (radius < 1.5f)
			return;

		var path = new PathF();
		var spikes = points * 2;
		for (var i = 0; i < spikes; i++)
		{
			var r = (i % 2 == 0) ? radius : radius * 0.38f;
			var a = rotation + i * (MathF.Tau / spikes) - MathF.PI / 2;
			var x = cx + MathF.Cos(a) * r;
			var y = cy + MathF.Sin(a) * r;
			if (i == 0)
				path.MoveTo(x, y);
			else
				path.LineTo(x, y);
		}

		path.Close();
		canvas.FillPath(path);
	}

	private void DrawBottomStatus(ICanvas canvas, RectF dirtyRect)
	{
		if (string.IsNullOrWhiteSpace(Frame.Status))
			return;

		var h = 40f;
		var y = dirtyRect.Bottom - h - 72;
		canvas.FillColor = Color.FromRgba(0.04f, 0.06f, 0.08f, 0.9f);
		canvas.FillRoundedRectangle(14, y, dirtyRect.Width - 28, h, 8);
		canvas.StrokeSize = 1;
		canvas.StrokeColor = Frame.IsLocked
			? Color.FromRgba(Mint.Red, Mint.Green, Mint.Blue, 0.5f)
			: Color.FromRgba(Cyan.Red, Cyan.Green, Cyan.Blue, 0.3f);
		canvas.DrawRoundedRectangle(14, y, dirtyRect.Width - 28, h, 8);
		canvas.Font = new Microsoft.Maui.Graphics.Font("ChakraPetch");
		canvas.FontSize = 13;
		canvas.FontColor = Colors.White;
		canvas.DrawString(Frame.Status, 26, y, dirtyRect.Width - 52, h,
			HorizontalAlignment.Left, VerticalAlignment.Center);
	}
}
