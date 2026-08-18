using System.Text.Json;
using GincanaHud.Shared;

namespace GincanaHud.App.Services;

public interface IActivityRouteCache
{
	void Save(Guid userId, ActivityDetailDto detail);
	ActivityDetailDto? TryGet(Guid activityId, Guid userId);
	void Clear();
}

/// <summary>Última ruta descargada, para arrancar el HUD sin cobertura.</summary>
public sealed class PreferencesActivityRouteCache : IActivityRouteCache
{
	private const string Key = "activity_route_cache_v1";
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
	};

	public void Save(Guid userId, ActivityDetailDto detail)
	{
		var json = JsonSerializer.Serialize(new CachedRoute(userId, detail), JsonOptions);
		Preferences.Default.Set(Key, json);
	}

	public ActivityDetailDto? TryGet(Guid activityId, Guid userId)
	{
		var raw = Preferences.Default.Get(Key, "");
		if (string.IsNullOrWhiteSpace(raw))
			return null;

		try
		{
			var cached = JsonSerializer.Deserialize<CachedRoute>(raw, JsonOptions);
			if (cached is null || cached.UserId != userId || cached.Detail.Id != activityId)
				return null;
			return cached.Detail;
		}
		catch
		{
			return null;
		}
	}

	public void Clear() => Preferences.Default.Remove(Key);

	private sealed record CachedRoute(Guid UserId, ActivityDetailDto Detail);
}
