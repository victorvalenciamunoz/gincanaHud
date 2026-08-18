using System.Net.Http.Json;
using System.Text.Json;
using GincanaHud.Shared;
using Microsoft.Extensions.Logging;

namespace GincanaHud.App.Services;

public interface IGincanaApiClient
{
	Task<UserDto> EnsureUserAsync(string displayName, CancellationToken ct = default);
	Task EnsureParticipantAsync(Guid activityId, Guid userId, CancellationToken ct = default);
	Task<JoinActivityResponse> JoinAsync(JoinActivityRequest request, CancellationToken ct = default);
	Task<ActivitySummaryDto?> GetActivityByCodeAsync(string joinCode, CancellationToken ct = default);
	Task<ActivityDetailDto?> GetActivityAsync(Guid activityId, Guid? userId = null, CancellationToken ct = default);
	Task<IReadOnlyList<ActivitySummaryDto>> GetActivitiesAsync(CancellationToken ct = default);
	Task<CaptureResponse> CaptureAsync(Guid activityId, CaptureRequest request, CancellationToken ct = default);
	Task<IReadOnlyList<RankingEntryDto>> GetRankingAsync(Guid activityId, CancellationToken ct = default);
}

public sealed class GincanaApiClient(HttpClient http, ILogger<GincanaApiClient> logger) : IGincanaApiClient
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
	};

	public async Task<UserDto> EnsureUserAsync(string displayName, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync("api/users", new UpsertUserRequest(displayName), ct);
		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions, ct))!;
	}

	public async Task EnsureParticipantAsync(Guid activityId, Guid userId, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync(
			$"api/activities/{activityId}/participants",
			new RegisterParticipantRequest(userId),
			ct);
		response.EnsureSuccessStatusCode();
	}

	public async Task<JoinActivityResponse> JoinAsync(JoinActivityRequest request, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync("api/activities/join", request, ct);
		if (!response.IsSuccessStatusCode)
			throw await CreateApiExceptionAsync("Unirse", response, ct);

		return (await response.Content.ReadFromJsonAsync<JoinActivityResponse>(JsonOptions, ct))!;
	}

	public async Task<ActivitySummaryDto?> GetActivityByCodeAsync(string joinCode, CancellationToken ct = default)
	{
		var code = Uri.EscapeDataString(joinCode.Trim());
		using var response = await http.GetAsync($"api/activities/by-code/{code}", ct);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			return null;
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<ActivitySummaryDto>(JsonOptions, ct);
	}

	public async Task<ActivityDetailDto?> GetActivityAsync(
		Guid activityId, Guid? userId = null, CancellationToken ct = default)
	{
		var url = userId is Guid uid
			? $"api/activities/{activityId}?userId={uid}"
			: $"api/activities/{activityId}";
		using var response = await http.GetAsync(url, ct);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			return null;
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<ActivityDetailDto>(JsonOptions, ct);
	}

	public async Task<IReadOnlyList<ActivitySummaryDto>> GetActivitiesAsync(CancellationToken ct = default)
	{
		var items = await http.GetFromJsonAsync<List<ActivitySummaryDto>>("api/activities", JsonOptions, ct);
		return items ?? [];
	}

	public async Task<CaptureResponse> CaptureAsync(
		Guid activityId, CaptureRequest request, CancellationToken ct = default)
	{
		using var response = await http.PostAsJsonAsync($"api/activities/{activityId}/capture", request, ct);
		if (!response.IsSuccessStatusCode)
			throw await CreateApiExceptionAsync("Captura", response, ct);

		return (await response.Content.ReadFromJsonAsync<CaptureResponse>(JsonOptions, ct))
			?? new CaptureResponse(false, 0, null, 0, null, "Respuesta vacía.");
	}

	private async Task<HttpRequestException> CreateApiExceptionAsync(
		string action, HttpResponseMessage response, CancellationToken ct)
	{
		var status = (int)response.StatusCode;
		var body = await response.Content.ReadAsStringAsync(ct);
		var problem = TryReadProblemDetails(body);
		var detail = problem?.Detail
			?? problem?.Title
			?? (string.IsNullOrWhiteSpace(body) ? null : body.Trim().Trim('"'));

		var message = string.IsNullOrWhiteSpace(detail)
			? $"{action} falló (HTTP {status})."
			: detail;

		logger.LogWarning(
			"{Action} → HTTP {Status} [{Code}]: {Detail}",
			action,
			status,
			problem?.Code ?? problem?.Title ?? "?",
			message);

		return new HttpRequestException(message, null, response.StatusCode);
	}

	private static ProblemDetailsPayload? TryReadProblemDetails(string body)
	{
		if (string.IsNullOrWhiteSpace(body) || body[0] is not '{')
			return null;
		try
		{
			return JsonSerializer.Deserialize<ProblemDetailsPayload>(body, JsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>Subset de RFC 7807 (+ extensión <c>code</c> de nuestra Api).</summary>
	private sealed class ProblemDetailsPayload
	{
		public string? Type { get; set; }
		public string? Title { get; set; }
		public int? Status { get; set; }
		public string? Detail { get; set; }
		public string? Instance { get; set; }
		public string? Code { get; set; }
	}

	public async Task<IReadOnlyList<RankingEntryDto>> GetRankingAsync(Guid activityId, CancellationToken ct = default)
	{
		var items = await http.GetFromJsonAsync<List<RankingEntryDto>>(
			$"api/activities/{activityId}/ranking", JsonOptions, ct);
		return items ?? [];
	}
}
