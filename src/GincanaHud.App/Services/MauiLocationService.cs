namespace GincanaHud.App.Services;

public sealed class MauiLocationService : ILocationService
{
    private CancellationTokenSource? _cts;

    public event EventHandler<GeoPosition>? PositionChanged;
    public GeoPosition? LastKnown { get; private set; }
    public bool IsListening => _cts is { IsCancellationRequested: false };

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsListening)
            return;

        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                throw new InvalidOperationException("Permiso de ubicación denegado.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;

        var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(request, token);
                    if (location is not null)
                    {
                        var position = new GeoPosition(
                            location.Latitude,
                            location.Longitude,
                            location.Accuracy,
                            DateTimeOffset.UtcNow);

                        LastKnown = position;
                        PositionChanged?.Invoke(this, position);
                    }
                }
                catch (Exception)
                {
                    // Keep listening; UI shows last known / status.
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1.5), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        return Task.CompletedTask;
    }
}
