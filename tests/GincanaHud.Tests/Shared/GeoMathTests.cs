using GincanaHud.Shared;

namespace GincanaHud.Tests.Shared;

// Neutral points around (0, 0): the repo is public (no real places).
public class GeoMathTests
{
	[Fact]
	public void Distance_to_the_same_point_is_zero()
	{
		Assert.Equal(0, GeoMath.DistanceMeters(0, 0, 0, 0), precision: 6);
	}

	[Fact]
	public void One_degree_of_latitude_is_about_111_km()
	{
		var meters = GeoMath.DistanceMeters(0, 0, 1, 0);

		Assert.InRange(meters, 111_000, 111_400);
	}

	[Fact]
	public void Distance_is_symmetric()
	{
		Assert.Equal(
			GeoMath.DistanceMeters(0.001, 0.002, -0.003, 0.004),
			GeoMath.DistanceMeters(-0.003, 0.004, 0.001, 0.002),
			precision: 6);
	}

	[Theory]
	[InlineData(1, 0, 0)]
	[InlineData(0, 1, 90)]
	[InlineData(-1, 0, 180)]
	[InlineData(0, -1, 270)]
	public void Bearing_points_to_the_target(double lat, double lon, double expected)
	{
		Assert.Equal(expected, GeoMath.BearingDegrees(0, 0, lat, lon), precision: 6);
	}

	[Theory]
	[InlineData(0, 10, 10)]
	[InlineData(10, 0, -10)]
	[InlineData(350, 10, 20)]
	[InlineData(10, 350, -20)]
	[InlineData(0, 180, 180)]
	public void Relative_bearing_is_the_shortest_signed_turn(double heading, double target, double expected)
	{
		var turn = GeoMath.RelativeBearingDegrees(heading, target);

		// 180 and -180 are the same turn.
		Assert.Equal(Math.Abs(expected), Math.Abs(turn), precision: 6);
		if (Math.Abs(expected) < 180)
			Assert.Equal(expected, turn, precision: 6);
	}

	[Fact]
	public void DestinationPoint_lands_at_the_requested_distance_and_bearing()
	{
		var (lat, lon) = GeoMath.DestinationPoint(0, 0, 45, 30);

		Assert.Equal(30, GeoMath.DistanceMeters(0, 0, lat, lon), precision: 3);
		Assert.Equal(45, GeoMath.BearingDegrees(0, 0, lat, lon), precision: 3);
	}
}
