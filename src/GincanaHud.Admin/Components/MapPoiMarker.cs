namespace GincanaHud.Admin.Components;

public sealed record MapPoiMarker(
	string Name,
	int Order,
	double Latitude,
	double Longitude,
	double RadiusMeters,
	int Points);
