using ErrorOr;
using GincanaHud.Api.Domain.ValueObjects;

namespace GincanaHud.Api.Domain.Pois;

public sealed class Poi
{
	private Poi() { }

	public Guid Id { get; private set; }
	public Guid OrganizationId { get; private set; }
	public Organizations.Organization Organization { get; private set; } = null!;
	public string Name { get; private set; } = "";
	public double Latitude { get; private set; }
	public double Longitude { get; private set; }
	public double RadiusMeters { get; private set; } = ValueObjects.RadiusMeters.Default;
	public string Clue { get; private set; } = "";
	public int DefaultPoints { get; private set; } = Points.Default;
	public DateTimeOffset CreatedAt { get; private set; }
	public List<Activities.ActivityPoi> ActivityLinks { get; private set; } = [];
	public List<Captures.Capture> Captures { get; private set; } = [];

	public GeoCoordinate Location => GeoCoordinate.FromPersistence(Latitude, Longitude);

	public static ErrorOr<Poi> Create(
		Guid organizationId,
		string? name,
		double latitude,
		double longitude,
		double radiusMeters,
		string? clue,
		int points,
		string nameFallback = "POI",
		double radiusFallback = ValueObjects.RadiusMeters.Default,
		Guid? id = null)
	{
		if (organizationId == Guid.Empty)
			return Error.Validation(code: "Poi.Organization", description: "Organización requerida.");

		var poiName = PoiName.Create(name, nameFallback);
		if (poiName.IsError) return poiName.Errors;

		var geo = GeoCoordinate.Create(latitude, longitude);
		if (geo.IsError) return geo.Errors;

		var radius = ValueObjects.RadiusMeters.Create(radiusMeters, radiusFallback);
		if (radius.IsError) return radius.Errors;

		var clueText = ClueText.Create(clue);
		if (clueText.IsError) return clueText.Errors;

		var pts = Points.Create(points);
		if (pts.IsError) return pts.Errors;

		return new Poi
		{
			Id = id ?? Guid.NewGuid(),
			OrganizationId = organizationId,
			Name = poiName.Value.Value,
			Latitude = geo.Value.Latitude,
			Longitude = geo.Value.Longitude,
			RadiusMeters = radius.Value.Value,
			Clue = clueText.Value.Value,
			DefaultPoints = pts.Value.Value,
			CreatedAt = DateTimeOffset.UtcNow
		};
	}

	public ErrorOr<Success> Update(
		string? name,
		double latitude,
		double longitude,
		double radiusMeters,
		string? clue,
		int points)
	{
		var poiName = PoiName.Create(name, Name);
		if (poiName.IsError) return poiName.Errors;

		var geo = GeoCoordinate.Create(latitude, longitude);
		if (geo.IsError) return geo.Errors;

		var radius = ValueObjects.RadiusMeters.Create(radiusMeters, RadiusMeters);
		if (radius.IsError) return radius.Errors;

		var clueText = ClueText.Create(clue, Clue);
		if (clueText.IsError) return clueText.Errors;

		var pts = Points.Create(points, DefaultPoints);
		if (pts.IsError) return pts.Errors;

		Name = poiName.Value.Value;
		Latitude = geo.Value.Latitude;
		Longitude = geo.Value.Longitude;
		RadiusMeters = radius.Value.Value;
		Clue = clueText.Value.Value;
		DefaultPoints = pts.Value.Value;
		return Result.Success;
	}
}
