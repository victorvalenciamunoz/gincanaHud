namespace GincanaHud.App.Services;

public sealed record GeoPosition(double Latitude, double Longitude, double? AccuracyMeters, DateTimeOffset Timestamp);

public interface ILocationService
{
    event EventHandler<GeoPosition>? PositionChanged;
    GeoPosition? LastKnown { get; }
    bool IsListening { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}
