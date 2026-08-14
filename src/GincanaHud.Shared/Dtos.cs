namespace GincanaHud.Shared;

public sealed record OrganizationDto(Guid Id, string Name, DateTimeOffset CreatedAt);

public sealed record CreateOrganizationRequest(string Name);

public sealed record UserDto(
	Guid Id,
	string DisplayName,
	string? ContactEmail,
	string? ContactPhone,
	DateTimeOffset CreatedAt);

/// <summary>Jugador con actividades en las que participa (vista Admin por organización).</summary>
public sealed record PlayerDto(
	Guid Id,
	string DisplayName,
	string? ContactEmail,
	string? ContactPhone,
	DateTimeOffset CreatedAt,
	IReadOnlyList<PlayerActivityDto> Activities);

public sealed record PlayerActivityDto(
	Guid ActivityId,
	string ActivityTitle,
	DateTimeOffset JoinedAt);

public sealed record UpsertUserRequest(string DisplayName, string? ContactEmail = null, string? ContactPhone = null);

public sealed record ActivitySummaryDto(
	Guid Id,
	string Title,
	string Description,
	bool IsActive,
	string JoinCode,
	ActivityRouteMode RouteMode,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	Guid OrganizationId,
	string? OrganizationName);

public sealed record CreateActivityRequest(
	Guid OrganizationId,
	string Title,
	string Description,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	ActivityRouteMode RouteMode = ActivityRouteMode.Sequential);

public sealed record UpdateActivityRequest(
	string Title,
	string Description,
	bool IsActive,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	ActivityRouteMode RouteMode = ActivityRouteMode.Sequential);

public sealed record ActivityDetailDto(
	Guid Id,
	string Title,
	string Description,
	bool IsActive,
	string JoinCode,
	ActivityRouteMode RouteMode,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	Guid OrganizationId,
	string OrganizationName,
	IReadOnlyList<ActivityPoiDto> Pois);

public sealed record ActivityPoiDto(
	Guid PoiId,
	string Name,
	int Order,
	double Latitude,
	double Longitude,
	double RadiusMeters,
	int Points,
	bool Captured,
	DateTimeOffset? CapturedAt,
	string? Clue);

public sealed record PoiDto(
	Guid Id,
	Guid OrganizationId,
	string Name,
	double Latitude,
	double Longitude,
	double RadiusMeters,
	int DefaultPoints,
	string? Clue);

public sealed record CreatePoiRequest(
	Guid OrganizationId,
	string Name,
	double Latitude,
	double Longitude,
	double RadiusMeters,
	string Clue,
	int Points);

public sealed record UpdateActivityPoiRequest(
	string Name,
	double Latitude,
	double Longitude,
	double RadiusMeters,
	string Clue,
	int Points,
	int Order);

public sealed record JoinActivityRequest(
	string JoinCode,
	string DisplayName,
	string? ContactEmail,
	string? ContactPhone);

public sealed record JoinActivityResponse(UserDto User, ActivitySummaryDto Activity);

public sealed record RegisterParticipantRequest(Guid UserId);

public sealed record CaptureRequest(
	Guid UserId,
	Guid PoiId,
	double Latitude,
	double Longitude,
	/// <summary>Hora real de captura (cola offline). Si null, usa reloj del servidor.</summary>
	DateTimeOffset? CapturedAt = null);

public sealed record CaptureResponse(
	bool Success,
	double DistanceMeters,
	string? Clue,
	int PointsAwarded,
	DateTimeOffset? CapturedAt,
	string Message);

public sealed record RankingEntryDto(
	Guid UserId,
	string DisplayName,
	string? ContactEmail,
	string? ContactPhone,
	int TotalPoints,
	int CaptureCount,
	DateTimeOffset? LastCaptureAt,
	/// <summary>Instante de captura del último POI de la ruta; null si aún no ha terminado.</summary>
	DateTimeOffset? FinishedAt);

public sealed record AdminLoginRequest(string Username, string Password);

public sealed record AdminLoginResponse(
	Guid Id,
	string Username,
	string Role,
	Guid? OrganizationId,
	string? OrganizationName,
	string AccessToken,
	DateTimeOffset ExpiresAt);

public sealed record AdminUserDto(
	Guid Id,
	string Username,
	string Role,
	Guid? OrganizationId,
	string? OrganizationName,
	bool IsActive,
	DateTimeOffset CreatedAt);

public sealed record CreateOrgAdminRequest(
	string Username,
	string Password,
	Guid OrganizationId);

public sealed record ClearPlayersResultDto(
	int CapturesDeleted,
	int ParticipantsDeleted,
	int UsersDeleted);

/// <summary>Snapshot en vivo del progreso de una actividad (Admin / premios).</summary>
public sealed record LiveProgressDto(
	Guid ActivityId,
	string Title,
	string JoinCode,
	ActivityRouteMode RouteMode,
	DateTimeOffset StartsAt,
	DateTimeOffset EndsAt,
	int PoiCount,
	int ParticipantCount,
	int FinisherCount,
	DateTimeOffset GeneratedAt,
	IReadOnlyList<LivePlayerProgressDto> Players);

public sealed record LivePlayerProgressDto(
	Guid UserId,
	string DisplayName,
	string? ContactEmail,
	string? ContactPhone,
	int CapturedCount,
	int PoiTotal,
	int? CurrentOrder,
	string? CurrentPoiName,
	DateTimeOffset? LastCaptureAt,
	DateTimeOffset? FinishedAt,
	int? FinishPlace,
	string Status,
	DateTimeOffset JoinedAt);
