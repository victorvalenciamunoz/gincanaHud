using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErrorOr;
using GincanaHud.Shared;

namespace GincanaHud.Admin.Services;

/// <summary>
/// The panel's HTTP client for the Api (blazor.md): business failures (4xx with the Api's reason) come back as
/// <see cref="ErrorOr{TValue}"/>; 401, 5xx and network failures stay exceptions.
/// </summary>
public sealed class AdminApiClient(HttpClient http)
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
	};

	public Task<ErrorOr<AdminLoginResponse>> LoginAsync(string username, string password, CancellationToken ct = default)
		=> SendAsync<AdminLoginResponse>(
			http.PostAsJsonAsync("api/panel-auth/login", new AdminLoginRequest(username, password), ct), ct);

	public Task<ErrorOr<List<AdminUserDto>>> GetAdminUsersAsync(CancellationToken ct = default)
		=> SendAsync<List<AdminUserDto>>(http.GetAsync("api/panel-users", ct), ct);

	public Task<ErrorOr<AdminUserDto>> CreateOrgAdminAsync(CreateOrgAdminRequest request, CancellationToken ct = default)
		=> SendAsync<AdminUserDto>(http.PostAsJsonAsync("api/panel-users", request, ct), ct);

	public Task<ErrorOr<List<PlayerDto>>> GetUsersAsync(Guid? organizationId = null, CancellationToken ct = default)
	{
		var url = organizationId is Guid oid
			? $"api/users?organizationId={oid}"
			: "api/users";
		return SendAsync<List<PlayerDto>>(http.GetAsync(url, ct), ct);
	}

	public Task<ErrorOr<UserDto>> UpsertUserAsync(string displayName, CancellationToken ct = default)
		=> SendAsync<UserDto>(http.PostAsJsonAsync("api/users", new UpsertUserRequest(displayName), ct), ct);

	public Task<ErrorOr<ClearPlayersResultDto>> ClearPlayersAsync(CancellationToken ct = default)
		=> SendAsync<ClearPlayersResultDto>(http.PostAsync("api/users/clear-players", null, ct), ct);

	public Task<ErrorOr<List<OrganizationDto>>> GetOrganizationsAsync(CancellationToken ct = default)
		=> SendAsync<List<OrganizationDto>>(http.GetAsync("api/organizations", ct), ct);

	public Task<ErrorOr<OrganizationDto>> CreateOrganizationAsync(string name, CancellationToken ct = default)
		=> SendAsync<OrganizationDto>(http.PostAsJsonAsync("api/organizations", new CreateOrganizationRequest(name), ct), ct);

	public Task<ErrorOr<List<ActivitySummaryDto>>> GetActivitiesAsync(bool includeInactive = false, CancellationToken ct = default)
	{
		var url = includeInactive ? "api/activities?includeInactive=true" : "api/activities";
		return SendAsync<List<ActivitySummaryDto>>(http.GetAsync(url, ct), ct);
	}

	public Task<ErrorOr<ActivitySummaryDto>> CreateActivityAsync(CreateActivityRequest request, CancellationToken ct = default)
		=> SendAsync<ActivitySummaryDto>(http.PostAsJsonAsync("api/activities", request, ct), ct);

	public Task<ErrorOr<ActivitySummaryDto>> UpdateActivityAsync(Guid id, UpdateActivityRequest request, CancellationToken ct = default)
		=> SendAsync<ActivitySummaryDto>(http.PutAsJsonAsync($"api/activities/{id}", request, ct), ct);

	public Task<ErrorOr<ActivityDetailDto>> GetActivityAsync(Guid id, CancellationToken ct = default)
		=> SendAsync<ActivityDetailDto>(http.GetAsync($"api/activities/{id}", ct), ct);

	public Task<ErrorOr<ActivityPoiDto>> AddPoiAsync(Guid activityId, CreatePoiRequest request, CancellationToken ct = default)
		=> SendAsync<ActivityPoiDto>(http.PostAsJsonAsync($"api/activities/{activityId}/pois", request, ct), ct);

	public Task<ErrorOr<ActivityPoiDto>> UpdatePoiAsync(
		Guid activityId, Guid poiId, UpdateActivityPoiRequest request, CancellationToken ct = default)
		=> SendAsync<ActivityPoiDto>(http.PutAsJsonAsync($"api/activities/{activityId}/pois/{poiId}", request, ct), ct);

	public Task<ErrorOr<Success>> RemovePoiAsync(Guid activityId, Guid poiId, CancellationToken ct = default)
		=> SendAsync(http.DeleteAsync($"api/activities/{activityId}/pois/{poiId}", ct), ct);

	public Task<ErrorOr<List<PoiDto>>> GetPoisAsync(Guid? organizationId = null, CancellationToken ct = default)
	{
		var url = organizationId is Guid id
			? $"api/pois?organizationId={id}"
			: "api/pois";
		return SendAsync<List<PoiDto>>(http.GetAsync(url, ct), ct);
	}

	public Task<ErrorOr<List<RankingEntryDto>>> GetRankingAsync(Guid activityId, CancellationToken ct = default)
		=> SendAsync<List<RankingEntryDto>>(http.GetAsync($"api/activities/{activityId}/ranking", ct), ct);

	public Task<ErrorOr<LiveProgressDto>> GetLiveProgressAsync(Guid activityId, CancellationToken ct = default)
		=> SendAsync<LiveProgressDto>(http.GetAsync($"api/activities/{activityId}/live", ct), ct);

	public Task<ErrorOr<List<GeocodeHitDto>>> SearchPlacesAsync(string query, CancellationToken ct = default)
		=> SendAsync<List<GeocodeHitDto>>(http.GetAsync($"api/places?q={Uri.EscapeDataString(query.Trim())}", ct), ct);

	private static async Task<ErrorOr<T>> SendAsync<T>(Task<HttpResponseMessage> request, CancellationToken ct)
	{
		using var response = await request;
		if (!response.IsSuccessStatusCode)
			return await ToErrorAsync(response, ct);

		var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
		if (value is null)
			throw new HttpRequestException("La Api respondió sin datos.", inner: null, response.StatusCode);
		return value;
	}

	private static async Task<ErrorOr<Success>> SendAsync(Task<HttpResponseMessage> request, CancellationToken ct)
	{
		using var response = await request;
		return response.IsSuccessStatusCode ? Result.Success : await ToErrorAsync(response, ct);
	}

	// 4xx (except 401) → Error with the Api's code and reason. 401 and 5xx → exception: the session handler already
	// closed the session on 401, and a 5xx is not a business rule.
	private static async Task<Error> ToErrorAsync(HttpResponseMessage response, CancellationToken ct)
	{
		var problem = await ReadProblemAsync(response, ct);
		var status = (int)response.StatusCode;
		var description = problem?.Detail ?? response.StatusCode switch
		{
			HttpStatusCode.Forbidden => "No tienes permiso para esto.",
			HttpStatusCode.NotFound => "No se ha encontrado.",
			_ => $"La Api respondió {status} ({response.ReasonPhrase})."
		};

		if (response.StatusCode == HttpStatusCode.Unauthorized || status >= 500)
			throw new HttpRequestException(description, inner: null, response.StatusCode);

		var code = problem?.Code ?? problem?.Title ?? $"Http.{status}";
		return response.StatusCode switch
		{
			HttpStatusCode.NotFound => Error.NotFound(code, description),
			HttpStatusCode.Conflict => Error.Conflict(code, description),
			HttpStatusCode.Forbidden => Error.Forbidden(code, description),
			_ => Error.Validation(code, description)
		};
	}

	private static async Task<ProblemDetailsBody?> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
	{
		try
		{
			return await response.Content.ReadFromJsonAsync<ProblemDetailsBody>(JsonOptions, ct);
		}
		catch (Exception ex) when (ex is JsonException or NotSupportedException)
		{
			// No ProblemDetails body (401/403 from the JWT middleware have none).
			return null;
		}
	}

	private sealed record ProblemDetailsBody(string? Title, string? Detail, string? Code);
}
