using Microsoft.Extensions.Logging;
using GincanaHud.Shared;

namespace GincanaHud.App.Services;

public interface ICaptureSyncService
{
	void Start();
	void Stop();
	Task FlushAsync(CancellationToken ct = default);
	event EventHandler? Flushed;
}

/// <summary>Reenvía la cola cuando la API vuelve a Online.</summary>
public sealed class CaptureSyncService : ICaptureSyncService, IDisposable
{
	private readonly ICaptureQueue _queue;
	private readonly IGincanaApiClient _api;
	private readonly IApiHealthMonitor _link;
	private readonly ILogger<CaptureSyncService>? _log;
	private int _flushing;
	private bool _started;

	public CaptureSyncService(
		ICaptureQueue queue,
		IGincanaApiClient api,
		IApiHealthMonitor link,
		ILogger<CaptureSyncService>? log = null)
	{
		_queue = queue;
		_api = api;
		_link = link;
		_log = log;
	}

	public event EventHandler? Flushed;

	public void Start()
	{
		if (_started)
			return;
		_started = true;
		_link.Changed += OnLinkChanged;
		_queue.Changed += OnQueueChanged;
		_link.Start();
		_ = FlushAsync();
	}

	public void Stop()
	{
		if (!_started)
			return;
		_started = false;
		_link.Changed -= OnLinkChanged;
		_queue.Changed -= OnQueueChanged;
	}

	public void Dispose() => Stop();

	private void OnLinkChanged(object? sender, EventArgs e)
	{
		if (_link.State == ApiLinkState.Online)
			_ = FlushAsync();
	}

	private void OnQueueChanged(object? sender, EventArgs e)
	{
		if (_link.State == ApiLinkState.Online && _queue.Count > 0)
			_ = FlushAsync();
	}

	public async Task FlushAsync(CancellationToken ct = default)
	{
		if (_link.State != ApiLinkState.Online)
			return;
		if (Interlocked.Exchange(ref _flushing, 1) == 1)
			return;

		try
		{
			var pending = _queue.Snapshot().OrderBy(p => p.CapturedAt).ToList();
			if (pending.Count == 0)
				return;

			var keep = new List<PendingCapture>();
			var removedAny = false;

			foreach (var item in pending)
			{
				ct.ThrowIfCancellationRequested();
				try
				{
					var response = await _api.CaptureAsync(
						item.ActivityId,
						new CaptureRequest(
							item.UserId,
							item.PoiId,
							item.Latitude,
							item.Longitude,
							item.CapturedAt),
						ct);

					if (response.Success)
					{
						removedAny = true;
						_log?.LogInformation(
							"Synced offline capture {PoiId} at {At}",
							item.PoiId,
							item.CapturedAt);
					}
					else
					{
						// Rechazo de negocio (p. ej. fuera de rango): no reintentar.
						removedAny = true;
						_log?.LogWarning(
							"Dropping queued capture {PoiId}: {Message}",
							item.PoiId,
							response.Message);
					}
				}
				catch (Exception ex) when (IsTransient(ex))
				{
					keep.Add(item with { Attempts = item.Attempts + 1 });
					// Red caída a mitad: conservar el resto sin tocar Attempts.
					var idx = pending.IndexOf(item);
					keep.AddRange(pending.Skip(idx + 1));
					_log?.LogDebug(ex, "Transient failure syncing capture {PoiId}", item.PoiId);
					break;
				}
				catch (Exception ex)
				{
					if (item.Attempts >= 5)
					{
						removedAny = true;
						_log?.LogWarning(ex, "Abandoning queued capture {PoiId}", item.PoiId);
					}
					else
					{
						keep.Add(item with { Attempts = item.Attempts + 1 });
					}
				}
			}

			_queue.ReplaceAll(keep);

			if (removedAny)
				Flushed?.Invoke(this, EventArgs.Empty);
		}
		finally
		{
			Interlocked.Exchange(ref _flushing, 0);
		}
	}

	private static bool IsTransient(Exception ex) =>
		ex is HttpRequestException or TaskCanceledException or TimeoutException
		|| ex.InnerException is HttpRequestException or TaskCanceledException;
}
