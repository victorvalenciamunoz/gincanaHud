using QRCoder;

namespace GincanaHud.Admin.Services;

public static class JoinQrGenerator
{
	/// <summary>PNG data-URI con el JoinCode (mismo texto que teclea el jugador).</summary>
	public static string ToPngDataUri(string joinCode, int pixelsPerModule = 8)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(joinCode);

		using var generator = new QRCodeGenerator();
		using var data = generator.CreateQrCode(joinCode.Trim().ToUpperInvariant(), QRCodeGenerator.ECCLevel.Q);
		var png = new PngByteQRCode(data);
		var bytes = png.GetGraphic(pixelsPerModule);
		return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
	}
}
