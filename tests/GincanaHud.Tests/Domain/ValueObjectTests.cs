using GincanaHud.Api.Domain.ValueObjects;

namespace GincanaHud.Tests.Domain;

public class ValueObjectTests
{
	[Theory]
	[InlineData("abc")]
	[InlineData("ABCDEFGHIJKLM")]
	[InlineData("   ")]
	[InlineData(null)]
	public void JoinCode_rejects_empty_or_out_of_range_codes(string? raw)
	{
		Assert.True(JoinCode.Create(raw).IsError);
	}

	[Fact]
	public void JoinCode_keeps_only_letters_and_digits_in_upper_case()
	{
		Assert.Equal("DEMO01", JoinCode.Create(" demo-01 ").Value.Value);
	}

	[Fact]
	public void DisplayName_is_trimmed()
	{
		Assert.Equal("Ana", DisplayName.Create("  Ana ").Value.Value);
	}

	[Fact]
	public void DisplayName_rejects_empty_and_too_long_names()
	{
		Assert.Equal("DisplayName.Empty", DisplayName.Create(" ").FirstError.Code);
		Assert.Equal("DisplayName.TooLong", DisplayName.Create(new string('a', DisplayName.MaxLength + 1)).FirstError.Code);
	}

	[Theory]
	[InlineData(90.1, 0)]
	[InlineData(-90.1, 0)]
	[InlineData(0, 180.1)]
	[InlineData(0, -180.1)]
	public void GeoCoordinate_rejects_out_of_range_values(double lat, double lon)
	{
		Assert.True(GeoCoordinate.Create(lat, lon).IsError);
	}

	[Fact]
	public void RadiusMeters_falls_back_to_the_default_and_caps_at_the_max()
	{
		Assert.Equal(RadiusMeters.Default, RadiusMeters.Create(0).Value.Value);
		Assert.Equal("Radius.TooLarge", RadiusMeters.Create(RadiusMeters.Max + 1).FirstError.Code);
	}

	[Fact]
	public void Points_falls_back_to_the_default_and_caps_at_the_max()
	{
		Assert.Equal(Points.Default, Points.Create(-5).Value.Value);
		Assert.Equal("Points.TooLarge", Points.Create(Points.Max + 1).FirstError.Code);
	}
}
