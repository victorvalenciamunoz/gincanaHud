namespace GincanaHud.Shared;

public static class GeoMath
{
	private const double EarthRadiusMeters = 6_371_000;

	public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = DegreesToRadians(lat2 - lat1);
		var dLon = DegreesToRadians(lon2 - lon1);
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
			+ Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
			* Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return EarthRadiusMeters * c;
	}

	public static double BearingDegrees(double lat1, double lon1, double lat2, double lon2)
	{
		var lat1R = DegreesToRadians(lat1);
		var lat2R = DegreesToRadians(lat2);
		var dLon = DegreesToRadians(lon2 - lon1);
		var y = Math.Sin(dLon) * Math.Cos(lat2R);
		var x = Math.Cos(lat1R) * Math.Sin(lat2R)
			- Math.Sin(lat1R) * Math.Cos(lat2R) * Math.Cos(dLon);
		var bearing = RadiansToDegrees(Math.Atan2(y, x));
		return (bearing + 360) % 360;
	}

	/// <summary>Signed shortest turn from heading to target bearing. Positive = turn right.</summary>
	public static double RelativeBearingDegrees(double headingDegrees, double targetBearingDegrees)
	{
		return (targetBearingDegrees - headingDegrees + 540d) % 360d - 180d;
	}

	/// <summary>Destination point roughly <paramref name="distanceMeters"/> along <paramref name="bearingDegrees"/>.</summary>
	public static (double Lat, double Lon) DestinationPoint(
		double lat, double lon, double bearingDegrees, double distanceMeters)
	{
		var angularDistance = distanceMeters / EarthRadiusMeters;
		var bearing = DegreesToRadians(bearingDegrees);
		var lat1 = DegreesToRadians(lat);
		var lon1 = DegreesToRadians(lon);

		var lat2 = Math.Asin(
			Math.Sin(lat1) * Math.Cos(angularDistance)
			+ Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearing));

		var lon2 = lon1 + Math.Atan2(
			Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(lat1),
			Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

		return (RadiansToDegrees(lat2), RadiansToDegrees(lon2));
	}

	private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
	private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;
}
