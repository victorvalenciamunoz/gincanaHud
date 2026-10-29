using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GincanaHud.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace GincanaHud.Api.Features.Places;

/// <summary>
/// Place search for the panel's map, following Nominatim's usage policy (D20): own User-Agent,
/// at most one request per second and cached results.
/// </summary>
public sealed class NominatimClient(HttpClient http, IMemoryCache cache)
{
	private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);
	private static readonly TimeSpan CacheFor = TimeSpan.FromDays(1);
	private static readonly SemaphoreSlim Gate = new(1, 1);
	private static DateTimeOffset _lastCall = DateTimeOffset.MinValue;

	public async Task<IReadOnlyList<GeocodeHitDto>> SearchAsync(string query, CancellationToken ct)
	{
		var key = "places:" + query.Trim().ToLowerInvariant();
		if (cache.TryGetValue(key, out IReadOnlyList<GeocodeHitDto>? cached))
			return cached!;

		await Gate.WaitAsync(ct);
		try
		{
			if (cache.TryGetValue(key, out cached))
				return cached!;

			var wait = _lastCall + MinInterval - DateTimeOffset.UtcNow;
			if (wait > TimeSpan.Zero)
				await Task.Delay(wait, ct);
			_lastCall = DateTimeOffset.UtcNow;

			var url = $"search?q={Uri.EscapeDataString(query.Trim())}&format=json&limit=6&addressdetails=0";
			var hits = await http.GetFromJsonAsync<List<NominatimHit>>(url, ct) ?? [];
			IReadOnlyList<GeocodeHitDto> result = hits
				.Where(h => TryParse(h.Lat, out _) && TryParse(h.Lon, out _))
				.Select(h =>
				{
					TryParse(h.Lat, out var lat);
					TryParse(h.Lon, out var lon);
					return new GeocodeHitDto(h.DisplayName ?? query, lat, lon);
				})
				.ToList();

			cache.Set(key, result, CacheFor);
			return result;
		}
		finally
		{
			Gate.Release();
		}
	}

	private static bool TryParse(string? value, out double number) =>
		double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

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
