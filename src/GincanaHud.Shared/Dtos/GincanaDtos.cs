namespace GincanaHud.Shared.Dtos;

public sealed record GincanaSummaryDto(Guid Id, string Title, string? Description, int CheckpointCount);

public sealed record GincanaDetailDto(
    Guid Id,
    string Title,
    string? Description,
    IReadOnlyList<CheckpointDto> Checkpoints);

public sealed record CheckpointDto(
    Guid Id,
    int Order,
    double Latitude,
    double Longitude,
    double RadiusMeters,
    int Points,
    string? Clue);

public sealed record ArriveRequest(Guid CheckpointId, double Latitude, double Longitude, string? PlayerName);

public sealed record ArriveResponse(
    bool Success,
    string Message,
    double DistanceMeters,
    string? Clue,
    int PointsAwarded);

public sealed record ScoreDto(Guid Id, string PlayerName, int TotalPoints, DateTimeOffset CompletedAt);

public sealed record SubmitScoreRequest(string PlayerName, int TotalPoints);
