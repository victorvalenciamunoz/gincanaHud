using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GincanaHud.Admin.Services;

public sealed class GeocodeService(HttpClient http)
{
	public async Task<IReadOnlyList<GeocodeHit>> SearchAsync(string query, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(query))
			return [];

		var url =
			$"search?q={Uri.EscapeDataString(query.Trim())}&format=json&limit=6&addressdetails=0";
		var hits = await http.GetFromJsonAsync<List<NominatimHit>>(url, ct);
		if (hits is null || hits.Count == 0)
			return [];

		return hits
			.Where(h => double.TryParse(h.Lat, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out _)
				&& double.TryParse(h.Lon, System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out _))
			.Select(h => new GeocodeHit(
				h.DisplayName ?? query,
				double.Parse(h.Lat!, System.Globalization.CultureInfo.InvariantCulture),
				double.Parse(h.Lon!, System.Globalization.CultureInfo.InvariantCulture)))
			.ToList();
	}

	private sealed class NominatimHit
	{
		[JsonPropertyName("lat")]
		public string? Lat { get; set; }

		[JsonPropertyName("lon")]
		public string? Lon { get; set; }

		[JsonPropertyName("display_name")]
		public string? DisplayName { get; set; }
	}
}

public sealed record GeocodeHit(string DisplayName, double Latitude, double Longitude);
