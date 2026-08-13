using Microsoft.Extensions.Logging;

namespace GincanaHud.App.Services;

public enum ApiLinkState
{
	Unknown,
	Offline,
	Unreachable,
	Online
}

public interface IApiHealthMonitor
{
	ApiLinkState State { get; }
	string Label { get; }
	string Detail { get; }
	event EventHandler? Changed;
	void Start();
	void Stop();
	Task ProbeAsync(CancellationToken ct = default);
}

/// <summary>Comprueba red del dispositivo + respuesta HTTP de la API (/health o base).</summary>
public sealed class ApiHealthMonitor : IApiHealthMonitor, IDisposable
{
	private readonly HttpClient _http;
	private readonly ILogger<ApiHealthMonitor>? _log;
	private readonly object _gate = new();
	private CancellationTokenSource? _loopCts;
	private int _probing;

	public ApiHealthMonitor(HttpClient http, ILogger<ApiHealthMonitor>? log = null)
	{
		_http = http;
		_log = log;
	}

	public ApiLinkState State { get; private set; } = ApiLinkState.Unknown;
	public string Label { get; private set; } = "…";
	public string Detail { get; private set; } = "Comprobando conexión…";

	public event EventHandler? Changed;

	public void Start()
	{
		lock (_gate)
		{
			if (_loopCts is not null)
				return;
			_loopCts = new CancellationTokenSource();
			_ = RunLoopAsync(_loopCts.Token);
		}
	}

	public void Stop()
	{
		CancellationTokenSource? cts;
		lock (_gate)
		{
			cts = _loopCts;
			_loopCts = null;
		}

		cts?.Cancel();
		cts?.Dispose();
	}

	public void Dispose() => Stop();

	public async Task ProbeAsync(CancellationToken ct = default)
	{
		if (Interlocked.Exchange(ref _probing, 1) == 1)
			return;

		try
		{
			if (Connectivity.Current.NetworkAccess is NetworkAccess.None or NetworkAccess.Unknown)
			{
				SetState(ApiLinkState.Offline, "Sin red", "El teléfono no tiene conexión a Internet.");
				return;
			}

			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(TimeSpan.FromSeconds(4));

			try
			{
				using var response = await _http.GetAsync("health", timeout.Token).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					SetState(ApiLinkState.Online, "API OK", "La API responde correctamente.");
					return;
				}

				// /health solo en Development: cualquier respuesta < 500 implica host vivo.
				if ((int)response.StatusCode < 500)
				{
					SetState(ApiLinkState.Online, "API OK", $"API alcanzable ({(int)response.StatusCode}).");
					return;
				}

				SetState(ApiLinkState.Unreachable, "Sin API", $"La API respondió {(int)response.StatusCode}.");
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				SetState(ApiLinkState.Unreachable, "Sin API", "La API no responde (tiempo agotado). ¿Dev Tunnel / Aspire?");
			}
			catch (Exception ex)
			{
				_log?.LogDebug(ex, "API health probe failed");
				SetState(ApiLinkState.Unreachable, "Sin API", "No se pudo contactar con la API. ¿Dev Tunnel activo?");
			}
		}
		finally
		{
			Interlocked.Exchange(ref _probing, 0);
		}
	}

	private async Task RunLoopAsync(CancellationToken ct)
	{
		await ProbeAsync(ct).ConfigureAwait(false);
		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(8));
		try
		{
			while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
				await ProbeAsync(ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			/* stopped */
		}
	}

	private void SetState(ApiLinkState state, string label, string detail)
	{
		if (State == state && Label == label && Detail == detail)
			return;

		State = state;
		Label = label;
		Detail = detail;
		Changed?.Invoke(this, EventArgs.Empty);
	}
}
