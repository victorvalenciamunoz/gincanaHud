namespace GincanaHud.App.Services;

public sealed record CompassReading(double HeadingDegrees, DateTimeOffset Timestamp);

public interface ICompassService
{
    event EventHandler<CompassReading>? HeadingChanged;
    double? LastHeadingDegrees { get; }
    bool IsListening { get; }
    void Start();
    void Stop();
}
