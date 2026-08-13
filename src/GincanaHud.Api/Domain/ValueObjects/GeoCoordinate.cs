using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class GeoCoordinate : IEquatable<GeoCoordinate>
{
	private GeoCoordinate(double latitude, double longitude)
	{
		Latitude = latitude;
		Longitude = longitude;
	}

	public double Latitude { get; }
	public double Longitude { get; }

	public static ErrorOr<GeoCoordinate> Create(double latitude, double longitude)
	{
		if (latitude is < -90 or > 90)
			return Error.Validation(code: "Geo.Latitude", description: "Latitud debe estar entre -90 y 90.");
		if (longitude is < -180 or > 180)
			return Error.Validation(code: "Geo.Longitude", description: "Longitud debe estar entre -180 y 180.");
		return new GeoCoordinate(latitude, longitude);
	}

	public static GeoCoordinate FromPersistence(double latitude, double longitude)
		=> new(latitude, longitude);

	public bool Equals(GeoCoordinate? other)
		=> other is not null
		   && Latitude.Equals(other.Latitude)
		   && Longitude.Equals(other.Longitude);

	public override bool Equals(object? obj) => obj is GeoCoordinate other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);
}
