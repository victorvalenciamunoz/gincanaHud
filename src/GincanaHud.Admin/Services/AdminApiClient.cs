using System.Net.Http.Json;
using System.Text.Json;
using GincanaHud.Shared;

namespace GincanaHud.Admin.Services;

public sealed class AdminApiClient(HttpClient http)
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
	};

	public async Task<AdminLoginResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync(
			"api/panel-auth/login",
			new AdminLoginRequest(username, password),
			ct);
		if (!response.IsSuccessStatusCode)
			return null;
		return await response.Content.ReadFromJsonAsync<AdminLoginResponse>(JsonOptions, ct);
	}

	public Task<List<AdminUserDto>?> GetAdminUsersAsync(CancellationToken ct = default)
		=> http.GetFromJsonAsync<List<AdminUserDto>>("api/panel-users", JsonOptions, ct);

	public async Task<AdminUserDto?> CreateOrgAdminAsync(CreateOrgAdminRequest request, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync("api/panel-users", request, ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<AdminUserDto>(JsonOptions, ct);
	}

	public Task<List<PlayerDto>?> GetUsersAsync(Guid? organizationId = null, CancellationToken ct = default)
	{
		var url = organizationId is Guid oid
			? $"api/users?organizationId={oid}"
			: "api/users";
		return http.GetFromJsonAsync<List<PlayerDto>>(url, JsonOptions, ct);
	}

	public async Task<UserDto?> UpsertUserAsync(string displayName, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync("api/users", new UpsertUserRequest(displayName), ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions, ct);
	}

	public async Task<ClearPlayersResultDto?> ClearPlayersAsync(CancellationToken ct = default)
	{
		using var response = await http.PostAsync("api/users/clear-players", null, ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<ClearPlayersResultDto>(JsonOptions, ct);
	}

	public Task<List<OrganizationDto>?> GetOrganizationsAsync(CancellationToken ct = default)
		=> http.GetFromJsonAsync<List<OrganizationDto>>("api/organizations", JsonOptions, ct);

	public async Task<OrganizationDto?> CreateOrganizationAsync(string name, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync("api/organizations", new CreateOrganizationRequest(name), ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<OrganizationDto>(JsonOptions, ct);
	}

	public Task<List<ActivitySummaryDto>?> GetActivitiesAsync(bool includeInactive = false, CancellationToken ct = default)
	{
		var url = includeInactive ? "api/activities?includeInactive=true" : "api/activities";
		return http.GetFromJsonAsync<List<ActivitySummaryDto>>(url, JsonOptions, ct);
	}

	public async Task<ActivitySummaryDto?> CreateActivityAsync(CreateActivityRequest request, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync("api/activities", request, ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<ActivitySummaryDto>(JsonOptions, ct);
	}

	public async Task<ActivitySummaryDto?> UpdateActivityAsync(Guid id, UpdateActivityRequest request, CancellationToken ct = default)
	{
		using var response = await http.PutAsJsonAsync($"api/activities/{id}", request, ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<ActivitySummaryDto>(JsonOptions, ct);
	}

	public Task<ActivityDetailDto?> GetActivityAsync(Guid id, CancellationToken ct = default)
		=> http.GetFromJsonAsync<ActivityDetailDto>($"api/activities/{id}", JsonOptions, ct);

	public async Task<ActivityPoiDto?> AddPoiAsync(Guid activityId, CreatePoiRequest request, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync($"api/activities/{activityId}/pois", request, ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<ActivityPoiDto>(JsonOptions, ct);
	}

	public async Task<ActivityPoiDto?> UpdatePoiAsync(
		Guid activityId, Guid poiId, UpdateActivityPoiRequest request, CancellationToken ct = default)
	{
		using var response = await http.PutAsJsonAsync($"api/activities/{activityId}/pois/{poiId}", request, ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<ActivityPoiDto>(JsonOptions, ct);
	}

	public async Task RemovePoiAsync(Guid activityId, Guid poiId, CancellationToken ct = default)
	{
		using var response = await http.DeleteAsync($"api/activities/{activityId}/pois/{poiId}", ct);
		await EnsureSuccessAsync(response, ct);
	}

	public Task<List<PoiDto>?> GetPoisAsync(Guid? organizationId = null, CancellationToken ct = default)
	{
		var url = organizationId is Guid id
			? $"api/pois?organizationId={id}"
			: "api/pois";
		return http.GetFromJsonAsync<List<PoiDto>>(url, JsonOptions, ct);
	}

	public Task<List<RankingEntryDto>?> GetRankingAsync(Guid activityId, CancellationToken ct = default)
		=> http.GetFromJsonAsync<List<RankingEntryDto>>($"api/activities/{activityId}/ranking", JsonOptions, ct);

	public Task<LiveProgressDto?> GetLiveProgressAsync(Guid activityId, CancellationToken ct = default)
		=> http.GetFromJsonAsync<LiveProgressDto>($"api/activities/{activityId}/live", JsonOptions, ct);

	public async Task<List<GeocodeHitDto>?> SearchPlacesAsync(string query, CancellationToken ct = default)
	{
		using var response = await http.GetAsync($"api/places?q={Uri.EscapeDataString(query.Trim())}", ct);
		await EnsureSuccessAsync(response, ct);
		return await response.Content.ReadFromJsonAsync<List<GeocodeHitDto>>(JsonOptions, ct);
	}

	// Like EnsureSuccessStatusCode, but the message is the Api's ProblemDetails "detail" when there is one,
	// so the pages show the reason ("La contraseña debe tener…") instead of just the status code.
	private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
	{
		if (response.IsSuccessStatusCode)
			return;

		string? detail = null;
		try
		{
			var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>(JsonOptions, ct);
			detail = problem?.Detail;
		}
		catch (Exception ex) when (ex is JsonException or NotSupportedException)
		{
			// Not a ProblemDetails body (401/403 have none): fall back to the status code.
		}

		throw new HttpRequestException(
			detail ?? $"La Api respondió {(int)response.StatusCode} ({response.ReasonPhrase}).",
			inner: null,
			response.StatusCode);
	}

	private sealed record ProblemDetailsBody(string? Detail);
}
