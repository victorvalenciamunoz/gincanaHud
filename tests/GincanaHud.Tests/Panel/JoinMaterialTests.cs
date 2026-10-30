using System.Buffers.Binary;
using GincanaHud.Admin.Services;

namespace GincanaHud.Tests.Panel;

/// <summary>What the organizer takes out of the panel for posters and messages.</summary>
public class JoinMaterialTests
{
	private static int PngWidth(string dataUri)
	{
		var png = Convert.FromBase64String(dataUri["data:image/png;base64,".Length..]);
		// The IHDR chunk starts at byte 8; the width follows its length and type.
		return BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
	}

	[Fact]
	public void The_downloaded_qr_is_big_enough_for_a_poster()
	{
		var uri = JoinQrGenerator.ToPngDataUri("ABC234", pixelsPerModule: 40);

		Assert.StartsWith("data:image/png;base64,", uri);
		Assert.True(PngWidth(uri) >= 1000, $"{PngWidth(uri)} px");
	}

	[Fact]
	public void The_invitation_carries_the_title_and_the_code_as_typed_in_the_app()
	{
		var text = JoinInvitation.Text(" Fiesta de prueba ", "abc234");

		Assert.Equal(
			"«Fiesta de prueba»: abre la app GincanaHud y únete con el código ABC234, o escanea el QR del cartel.",
			text);
	}
}
