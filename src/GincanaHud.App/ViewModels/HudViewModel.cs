using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GincanaHud.App.Hud;
using GincanaHud.App.Services;
using GincanaHud.Shared;

namespace GincanaHud.App.ViewModels;

public sealed class HudViewModel : ObservableObject, IDisposable
{
	public const double ProximityMeters = 15;
	public const double LockAngleDegrees = 15;

	private readonly ILocationService _location;
	private readonly ICompassService _compass;
	private readonly IOrientationService _orientation;
	private readonly IGincanaApiClient _api;
	private readonly IJoinSessionStore _session;
	private readonly ICaptureFxService _captureFx;
	private readonly IApiHealthMonitor _link;
	private readonly ICaptureQueue _captureQueue;
	private IDispatcherTimer? _pulseTimer;

	private double? _latitude;
	private double? _longitude;
	private double? _headingDegrees;
	private double _pitchDegrees;
	private double? _distanceMeters;
	private double _relativeBearingDegrees;
	private double _targetBearingDegrees;
	private bool _isInProximity;
	private bool _isLocked;
	private bool _isRunning;
	private string _status = "HUD listo";
	private string _clue = "";
	private double _pulse;
	private string _targetLabel = "Sin objetivo";
	private string _progressText = "";
	private double _progressRatio;
	private bool _hasProgress;
	private bool _progressDetailVisible;
	private string _progressDetailText = "";
	private IDispatcherTimer? _progressDetailTimer;
	private string _linkLabel = "…";
	private string _linkColor = "#6B7C8D";
	private string _linkDetail = "";
	private bool _linkDetailVisible;

	private double? _targetLat;
	private double? _targetLon;
	private double _targetRadiusMeters = 25;
	private string _targetClue = "Objetivo de prueba.";
	private Guid? _activityId;
	private Guid? _poiId;
	private Guid? _userId;
	private bool _useApiCapture;
	private bool _serverConfirmed;
	private bool _arriveInFlight;
	private int _poiTotal;
	private int _poiCaptured;
	private List<ActivityPoiDto> _routePois = [];
	private ActivityRouteMode _routeMode = ActivityRouteMode.Sequential;

	public HudViewModel(
		ILocationService location,
		ICompassService compass,
		IOrientationService orientation,
		IGincanaApiClient api,
		IJoinSessionStore session,
		ICaptureFxService captureFx,
		IApiHealthMonitor link,
		ICaptureQueue captureQueue)
	{
		_location = location;
		_compass = compass;
		_orientation = orientation;
		_api = api;
		_session = session;
		_captureFx = captureFx;
		_link = link;
		_captureQueue = captureQueue;
		Frame = new HudFrame();
		Drawable = new HudDrawable { Frame = Frame };
		ProgressRing = new ProgressRingDrawable();

		ToggleCommand = new Command(async () => await ToggleAsync());
		ToggleProgressDetailCommand = new Command(ShowProgressDetail);
		ToggleLinkDetailCommand = new Command(ShowLinkDetail);

		_location.PositionChanged += OnPositionChanged;
		_compass.HeadingChanged += OnHeadingChanged;
		_orientation.PitchChanged += OnPitchChanged;
		_link.Changed += OnLinkChanged;
		_captureQueue.Changed += OnCaptureQueueChanged;
		SyncLinkFromMonitor();

		_useApiCapture = false;
		TargetLabel = "Sin objetivo";
		Status = "Unirse → Iniciar";
	}

	public event EventHandler? FrameUpdated;
	public event EventHandler? CameraStartRequested;
	public event EventHandler? CameraStopRequested;
	public event EventHandler<RouteFinishedEventArgs>? RouteFinished;

	public HudFrame Frame { get; }
	public HudDrawable Drawable { get; }
	public ProgressRingDrawable ProgressRing { get; }

	public event EventHandler? ProgressRingInvalidateRequested;

	public double? Latitude
	{
		get => _latitude;
		private set => SetProperty(ref _latitude, value);
	}

	public double? Longitude
	{
		get => _longitude;
		private set => SetProperty(ref _longitude, value);
	}

	public double? HeadingDegrees
	{
		get => _headingDegrees;
		private set => SetProperty(ref _headingDegrees, value);
	}

	public double? DistanceMeters
	{
		get => _distanceMeters;
		private set => SetProperty(ref _distanceMeters, value);
	}

	public double RelativeBearingDegrees
	{
		get => _relativeBearingDegrees;
		private set => SetProperty(ref _relativeBearingDegrees, value);
	}

	public double TargetBearingDegrees
	{
		get => _targetBearingDegrees;
		private set => SetProperty(ref _targetBearingDegrees, value);
	}

	public bool IsInProximity
	{
		get => _isInProximity;
		private set => SetProperty(ref _isInProximity, value);
	}

	public bool IsLocked
	{
		get => _isLocked;
		private set => SetProperty(ref _isLocked, value);
	}

	public bool IsRunning
	{
		get => _isRunning;
		private set => SetProperty(ref _isRunning, value);
	}

	public string Status
	{
		get => _status;
		set => SetProperty(ref _status, value);
	}

	public string Clue
	{
		get => _clue;
		private set => SetProperty(ref _clue, value);
	}

	public double Pulse
	{
		get => _pulse;
		private set => SetProperty(ref _pulse, value);
	}

	public string TargetLabel
	{
		get => _targetLabel;
		private set => SetProperty(ref _targetLabel, value);
	}

	/// <summary>Ej. "2/5" en el centro del anillo.</summary>
	public string ProgressText
	{
		get => _progressText;
		private set => SetProperty(ref _progressText, value);
	}

	public double ProgressRatio
	{
		get => _progressRatio;
		private set
		{
			if (SetProperty(ref _progressRatio, value))
			{
				ProgressRing.Progress = value;
				ProgressRingInvalidateRequested?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public bool HasProgress
	{
		get => _hasProgress;
		private set => SetProperty(ref _hasProgress, value);
	}

	public bool ProgressDetailVisible
	{
		get => _progressDetailVisible;
		private set => SetProperty(ref _progressDetailVisible, value);
	}

	public string ProgressDetailText
	{
		get => _progressDetailText;
		private set => SetProperty(ref _progressDetailText, value);
	}

	public string LinkLabel
	{
		get => _linkLabel;
		private set => SetProperty(ref _linkLabel, value);
	}

	public string LinkColor
	{
		get => _linkColor;
		private set => SetProperty(ref _linkColor, value);
	}

	public string LinkDetail
	{
		get => _linkDetail;
		private set => SetProperty(ref _linkDetail, value);
	}

	public bool LinkDetailVisible
	{
		get => _linkDetailVisible;
		private set => SetProperty(ref _linkDetailVisible, value);
	}

	public ICommand ToggleCommand { get; }
	public ICommand ToggleProgressDetailCommand { get; }
	public ICommand ToggleLinkDetailCommand { get; }

	public void StartLinkMonitor() => _link.Start();

	public void StopLinkMonitor() => _link.Stop();

	public void Dispose()
	{
		if (_pulseTimer is not null)
		{
			_pulseTimer.Stop();
			_pulseTimer.Tick -= OnPulseTick;
			_pulseTimer = null;
		}

		_location.PositionChanged -= OnPositionChanged;
		_compass.HeadingChanged -= OnHeadingChanged;
		_orientation.PitchChanged -= OnPitchChanged;
		_link.Changed -= OnLinkChanged;
		_captureQueue.Changed -= OnCaptureQueueChanged;
		_link.Stop();
		_progressDetailTimer?.Stop();
		_progressDetailTimer = null;
	}

	private IDispatcherTimer EnsurePulseTimer()
	{
		if (_pulseTimer is not null)
			return _pulseTimer;

		var dispatcher = Application.Current?.Dispatcher
			?? throw new InvalidOperationException("Dispatcher no disponible aún.");

		_pulseTimer = dispatcher.CreateTimer();
		_pulseTimer.Interval = TimeSpan.FromMilliseconds(50);
		_pulseTimer.Tick += OnPulseTick;
		return _pulseTimer;
	}

	private async Task ToggleAsync()
	{
		if (IsRunning)
		{
			await StopAsync();
			return;
		}

		await StartAsync();
	}

	/// <summary>Arranque idempotente (Unirse → HUD, o cold start con sesión).</summary>
	public Task EnsureStartedAsync()
	{
		if (IsRunning)
			return Task.CompletedTask;
		return StartAsync();
	}

	private async Task StartAsync()
	{
		try
		{
			var join = _session.Current;
			if (join is null)
			{
				Status = "Primero únete (pestaña Unirse).";
				return;
			}

			var camera = await Permissions.RequestAsync<Permissions.Camera>();
			if (camera != PermissionStatus.Granted)
			{
				Status = "Permiso de cámara denegado.";
				return;
			}

			// Tras vaciar BD / reiniciar Aspire el UserId local puede quedar huérfano.
			Status = "Sincronizando sesión…";
			join = await RefreshJoinSessionAsync(join);
			if (join is null)
			{
				Status = "No se pudo renovar la sesión. Vuelve a Unirse.";
				return;
			}

			Status = "Cargando actividad…";
			var detail = await _api.GetActivityAsync(join.ActivityId, join.UserId);
			if (detail is null)
			{
				_session.ClearSession();
				Status = "La sesión ya no es válida (¿se reinició el servidor?). Vuelve a Unirse — tus datos están guardados.";
				return;
			}

			_routePois = MergeQueuedCaptures(
				detail.Pois.OrderBy(p => p.Order).ToList(),
				detail.Id,
				join.UserId);
			_routeMode = detail.RouteMode;
			_activityId = detail.Id;
			_userId = join.UserId;

			var next = PickNextPoi();

			if (next is null)
			{
				if (_routePois.Count == 0)
				{
					Status = "La actividad no tiene POIs aún.";
					TargetLabel = detail.Title;
					Clue = "";
					_useApiCapture = false;
					_targetLat = null;
					_targetLon = null;
					ApplyProgress(_routePois, current: null);
				}
				else
				{
					// Ya estaba todo capturado: mostrar fin + ranking.
					ApplyProgress(_routePois, current: null);
					await CompleteRouteAsync(detail.Title);
					return;
				}
			}
			else
			{
				ApplyTarget(join.UserId, detail.Id, next);
				ApplyProgress(_routePois, next);
				Status = _routeMode == ActivityRouteMode.Free
					? "Ruta libre — objetivo más cercano"
					: "Objetivo listo";
				PushFrame();
			}

			Status = "Iniciando sensores…";
			await _location.StartAsync();
			_compass.Start();
			_orientation.Start();
			EnsurePulseTimer().Start();
			IsRunning = true;
			if (_targetLat is not null)
				Status = "HUD activo";
			CameraStartRequested?.Invoke(this, EventArgs.Empty);
			Recalculate();
		}
		catch (Exception ex)
		{
			IsRunning = false;
			Status = ex.Message;
		}
	}

	/// <summary>Rehace join por nombre/código para obtener UserId e inscripción vigentes.</summary>
	private async Task<JoinSession?> RefreshJoinSessionAsync(JoinSession join)
	{
		var profile = _session.LastProfile;
		var code = FirstNonEmpty(join.JoinCode, profile.JoinCode);
		var name = FirstNonEmpty(join.DisplayName, profile.DisplayName);
		if (code is null || name is null)
			return join;

		try
		{
			var result = await _api.JoinAsync(new JoinActivityRequest(
				code,
				name,
				string.IsNullOrWhiteSpace(profile.ContactEmail) ? null : profile.ContactEmail,
				string.IsNullOrWhiteSpace(profile.ContactPhone) ? null : profile.ContactPhone));

			var refreshed = new JoinSession(
				result.User.Id,
				result.Activity.Id,
				result.Activity.Title,
				name,
				code);
			_session.Save(refreshed, profile with { DisplayName = name, JoinCode = code });
			return refreshed;
		}
		catch
		{
			// Si falla (p. ej. sin red), seguimos con la sesión local; Capture mostrará el error.
			return join;
		}
	}

	private static string? FirstNonEmpty(params string[] values)
	{
		foreach (var v in values)
		{
			if (!string.IsNullOrWhiteSpace(v))
				return v.Trim();
		}

		return null;
	}

	private void ApplyTarget(Guid userId, Guid activityId, ActivityPoiDto poi)
	{
		_useApiCapture = true;
		_userId = userId;
		_activityId = activityId;
		_poiId = poi.PoiId;
		_serverConfirmed = false;
		_targetLat = poi.Latitude;
		_targetLon = poi.Longitude;
		_targetRadiusMeters = poi.RadiusMeters;
		_targetClue = string.IsNullOrWhiteSpace(poi.Clue) ? "" : poi.Clue.Trim();
		// No mostrar el nombre del POI: puede spoilear la pista.
		TargetLabel = _routeMode == ActivityRouteMode.Free
			? "Punto más cercano"
			: "Siguiente punto";
		Clue = _targetClue;
		IsLocked = false;
	}

	/// <summary>
	/// Secuencial: primer POI pendiente por Order.
	/// Libre: pendiente más cercano (si aún no hay GPS, por Order).
	/// </summary>
	private ActivityPoiDto? PickNextPoi()
	{
		var pending = _routePois.Where(p => !p.Captured).ToList();
		if (pending.Count == 0)
			return null;

		if (_routeMode == ActivityRouteMode.Sequential)
			return pending.OrderBy(p => p.Order).First();

		if (Latitude is not double lat || Longitude is not double lon)
			return pending.OrderBy(p => p.Order).First();

		return pending
			.OrderBy(p => GeoMath.DistanceMeters(lat, lon, p.Latitude, p.Longitude))
			.First();
	}

	/// <summary>En ruta libre, cambia de objetivo si otro pendiente está claramente más cerca.</summary>
	private void MaybeRetargetNearest()
	{
		if (_routeMode != ActivityRouteMode.Free || !_useApiCapture)
			return;
		if (_arriveInFlight || _serverConfirmed || IsLocked)
			return;
		if (_userId is null || _activityId is null)
			return;
		if (Latitude is not double lat || Longitude is not double lon)
			return;

		var next = PickNextPoi();
		if (next is null || next.PoiId == _poiId)
			return;

		if (_poiId is Guid currentId)
		{
			var current = _routePois.FirstOrDefault(p => p.PoiId == currentId && !p.Captured);
			if (current is not null)
			{
				var dCur = GeoMath.DistanceMeters(lat, lon, current.Latitude, current.Longitude);
				var dNew = GeoMath.DistanceMeters(lat, lon, next.Latitude, next.Longitude);
				// Histéresis: hace falta ~12 m de ventaja para no bailar con el GPS.
				if (dNew >= dCur - 12)
					return;
			}
		}

		ApplyTarget(_userId.Value, _activityId.Value, next);
		ApplyProgress(_routePois, next);
		Status = "Nuevo objetivo cercano";
	}

	/// <summary>
	/// Muestra progreso de ruta: objetivo actual / total (p. ej. 2 / 5).
	/// Si no hay objetivo pendiente, capturados / total.
	/// </summary>
	private void ApplyProgress(IReadOnlyList<ActivityPoiDto> pois, ActivityPoiDto? current)
	{
		_poiTotal = pois.Count;
		_poiCaptured = pois.Count(p => p.Captured);
		if (_poiTotal <= 0)
		{
			ClearProgressUi();
			return;
		}

		HasProgress = true;
		ProgressRatio = _poiCaptured / (double)_poiTotal;

		if (_routeMode == ActivityRouteMode.Free || current is null)
		{
			ProgressText = $"{_poiCaptured}/{_poiTotal}";
			return;
		}

		var index = pois.OrderBy(p => p.Order).ToList().FindIndex(p => p.PoiId == current.PoiId) + 1;
		if (index <= 0)
			index = Math.Min(_poiCaptured + 1, _poiTotal);
		ProgressText = $"{index}/{_poiTotal}";
	}

	private void ClearProgressUi()
	{
		HasProgress = false;
		ProgressRatio = 0;
		ProgressText = "";
		ProgressDetailVisible = false;
		ProgressDetailText = "";
	}

	private void ShowProgressDetail()
	{
		if (!HasProgress || _poiTotal <= 0)
			return;

		var remaining = Math.Max(0, _poiTotal - _poiCaptured);
		ProgressDetailText = remaining == 0
			? "¡Ruta completada!"
			: remaining == 1
				? "Te queda 1 punto"
				: $"Te quedan {remaining} puntos";
		ProgressDetailVisible = true;
		LinkDetailVisible = false;

		_progressDetailTimer ??= CreateDetailTimer();
		_progressDetailTimer.Stop();
		_progressDetailTimer.Start();
	}

	private void ShowLinkDetail()
	{
		LinkDetail = _link.Detail;
		LinkDetailVisible = true;
		ProgressDetailVisible = false;

		_progressDetailTimer ??= CreateDetailTimer();
		_progressDetailTimer.Stop();
		_progressDetailTimer.Start();
	}

	private void OnLinkChanged(object? sender, EventArgs e)
		=> MainThread.BeginInvokeOnMainThread(SyncLinkFromMonitor);

	private void SyncLinkFromMonitor()
	{
		LinkLabel = _link.Label;
		var pending = _captureQueue.Count;
		LinkDetail = pending > 0
			? $"{_link.Detail} · {pending} captura{(pending == 1 ? "" : "s")} pendiente{(pending == 1 ? "" : "s")}"
			: _link.Detail;
		LinkColor = _link.State switch
		{
			ApiLinkState.Online => "#7CFFB2",
			ApiLinkState.Offline => "#FF5C5C",
			ApiLinkState.Unreachable => "#FFC42E",
			_ => "#6B7C8D"
		};
	}

	private void OnCaptureQueueChanged(object? sender, EventArgs e)
		=> MainThread.BeginInvokeOnMainThread(SyncLinkFromMonitor);

	private IDispatcherTimer CreateDetailTimer()
	{
		var dispatcher = Application.Current?.Dispatcher
			?? throw new InvalidOperationException("Dispatcher no disponible aún.");
		var timer = dispatcher.CreateTimer();
		timer.Interval = TimeSpan.FromSeconds(2.4);
		timer.Tick += (_, _) =>
		{
			timer.Stop();
			ProgressDetailVisible = false;
			LinkDetailVisible = false;
		};
		return timer;
	}

	private async Task StopAsync()
	{
		_pulseTimer?.Stop();
		await _location.StopAsync();
		_compass.Stop();
		_orientation.Stop();
		IsRunning = false;
		IsLocked = false;
		IsInProximity = false;
		Clue = "";
		ClearProgressUi();
		Status = "Detenido";
		CameraStopRequested?.Invoke(this, EventArgs.Empty);
		PushFrame();
	}

	private void OnPositionChanged(object? sender, GeoPosition pos)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			Latitude = pos.Latitude;
			Longitude = pos.Longitude;
			Recalculate();
		});
	}

	private void OnHeadingChanged(object? sender, CompassReading reading)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			HeadingDegrees = reading.HeadingDegrees;
			Recalculate();
		});
	}

	private void OnPitchChanged(object? sender, OrientationReading reading)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			_pitchDegrees = reading.PitchDegrees;
			Recalculate();
		});
	}

	private double _captureFlash;

	private void OnPulseTick(object? sender, EventArgs e)
	{
		if (_captureFlash > 0)
		{
			// ~1.6 s de explosión de estrellas
			_captureFlash = Math.Max(0, _captureFlash - 0.032);
		}

		if (!IsInProximity && !IsLocked && _captureFlash <= 0)
		{
			Pulse = 0;
			PushFrame();
			return;
		}

		if (IsInProximity || IsLocked)
		{
			var distance = DistanceMeters ?? ProximityMeters;
			var speed = 1.0 + Math.Clamp((ProximityMeters - distance) / ProximityMeters, 0, 1) * 3.0;
			Pulse = (Pulse + 0.05 * speed) % 1.0;
			if (IsInProximity && !IsLocked)
				_captureFx.TickProximity(distance);
		}

		PushFrame();
	}

	private void Recalculate()
	{
		if (_routeMode == ActivityRouteMode.Free
			&& _useApiCapture
			&& _poiId is null
			&& Latitude is not null
			&& Longitude is not null
			&& _userId is Guid uid
			&& _activityId is Guid aid)
		{
			var bootstrap = PickNextPoi();
			if (bootstrap is not null)
			{
				ApplyTarget(uid, aid, bootstrap);
				ApplyProgress(_routePois, bootstrap);
			}
		}

		if (_targetLat is null || _targetLon is null || Latitude is null || Longitude is null)
			return;

		MaybeRetargetNearest();

		var distance = GeoMath.DistanceMeters(
			Latitude.Value, Longitude.Value, _targetLat.Value, _targetLon.Value);
		DistanceMeters = distance;

		var bearing = GeoMath.BearingDegrees(
			Latitude.Value, Longitude.Value, _targetLat.Value, _targetLon.Value);
		TargetBearingDegrees = bearing;

		if (HeadingDegrees is double heading)
		{
			var rawRel = GeoMath.RelativeBearingDegrees(heading, bearing);
			// Suavizado extra del rumbo relativo (el marcador deja de “bailar”).
			RelativeBearingDegrees = SmoothRelativeBearing(RelativeBearingDegrees, rawRel, 0.28);
		}

		IsInProximity = distance < ProximityMeters;
		var withinRadius = distance <= _targetRadiusMeters;
		var aligned = Math.Abs(RelativeBearingDegrees) <= LockAngleDegrees;
		var locked = withinRadius && aligned && HeadingDegrees is not null;

		if (locked != IsLocked)
		{
			IsLocked = locked;
			if (locked)
			{
				if (_useApiCapture)
				{
					Status = "LOCK — validando en API…";
					_ = ConfirmArriveAsync();
				}
				else
				{
					Status = "LOCK — local";
					_captureFlash = 1;
					_captureFx.PlayCaptureSuccess();
				}
			}
			else
			{
				_serverConfirmed = false;
			}
		}

		if (!IsLocked)
		{
			Status = HeadingDegrees is null
				? "Esperando brújula…"
				: FormatSteerStatus(RelativeBearingDegrees, distance, withinRadius);
		}
		else if (_serverConfirmed && Clue.Length > 0)
		{
			Status = "LOCK — confirmado servidor";
		}

		PushFrame();
	}

	/// <summary>
	/// Rumbo relativo: + derecha, − izquierda.
	/// </summary>
	internal static string FormatSteerStatus(double relativeBearingDegrees, double distanceMeters, bool withinRadius)
	{
		var abs = Math.Abs(relativeBearingDegrees);
		var side = relativeBearingDegrees >= 0 ? "derecha" : "izquierda";
		var dist = distanceMeters >= 1000
			? $"{distanceMeters / 1000:0.0} km"
			: $"{distanceMeters:0} m";

		string turn;
		if (abs <= LockAngleDegrees)
			turn = "De frente";
		else if (abs <= 45)
			turn = $"Un poco a la {side}";
		else if (abs <= 135)
			turn = $"Gira a la {side}";
		else
			turn = $"Date la vuelta ({side})";

		if (withinRadius)
			return abs <= LockAngleDegrees
				? "En rango — apunta al objetivo"
				: $"En rango — {turn.ToLowerInvariant()}";

		return abs <= LockAngleDegrees
			? $"De frente · {dist}"
			: $"{turn} · {dist}";
	}

	private async Task ConfirmArriveAsync()
	{
		if (!_useApiCapture || _arriveInFlight || _serverConfirmed)
			return;
		if (_activityId is null || _poiId is null || _userId is null || Latitude is null || Longitude is null)
			return;

		_arriveInFlight = true;
		var activityId = _activityId.Value;
		var poiId = _poiId.Value;
		var userId = _userId.Value;
		var lat = Latitude.Value;
		var lon = Longitude.Value;
		var capturedAt = DateTimeOffset.UtcNow;

		try
		{
			var response = await _api.CaptureAsync(
				activityId,
				new CaptureRequest(userId, poiId, lat, lon, capturedAt));

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (response.Success)
				{
					await AcceptCaptureAsync(
						poiId,
						response.CapturedAt ?? capturedAt,
						response.PointsAwarded,
						pendingSync: false);
				}
				else
				{
					Status = $"API: {response.Message}";
					PushFrame();
				}
			});
		}
		catch (Exception ex) when (IsTransientCaptureFailure(ex))
		{
			_captureQueue.Enqueue(new PendingCapture(
				Guid.NewGuid(),
				activityId,
				userId,
				poiId,
				lat,
				lon,
				capturedAt));

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await AcceptCaptureAsync(poiId, capturedAt, pointsAwarded: 0, pendingSync: true);
			});
		}
		catch (Exception ex)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Status = ex.Message.Contains("unirte", StringComparison.OrdinalIgnoreCase)
					? "Sesión caducada — Detener, Unirse de nuevo e Iniciar."
					: ex.Message;
				PushFrame();
			});
		}
		finally
		{
			_arriveInFlight = false;
		}
	}

	private static bool IsTransientCaptureFailure(Exception ex) =>
		ex is HttpRequestException or TaskCanceledException or TimeoutException
		|| ex.InnerException is HttpRequestException or TaskCanceledException;

	private async Task AcceptCaptureAsync(
		Guid poiId,
		DateTimeOffset capturedAt,
		int pointsAwarded,
		bool pendingSync)
	{
		_serverConfirmed = true;
		_captureFlash = 1;
		_captureFx.PlayCaptureSuccess();
		_routePois = MarkPoiCaptured(_routePois, poiId, capturedAt);

		Status = pendingSync
			? "Capturado sin red — se sincronizará"
			: pointsAwarded > 0
				? $"Capturado +{pointsAwarded} pts {capturedAt.ToLocalTime():HH:mm:ss}"
				: $"Capturado {capturedAt.ToLocalTime():HH:mm:ss}";

		if (pendingSync)
			await AdvanceFromLocalRouteAsync();
		else
			await TryAdvanceToNextPoiAsync();

		PushFrame();
	}

	private async Task AdvanceFromLocalRouteAsync()
	{
		if (_activityId is null || _userId is null)
			return;

		var next = PickNextPoi();
		if (next is null)
		{
			var title = _session.Current?.ActivityTitle ?? "Actividad";
			ApplyProgress(_routePois, current: null);
			await CompleteRouteAsync(title);
			return;
		}

		ApplyTarget(_userId.Value, _activityId.Value, next);
		ApplyProgress(_routePois, next);
		Status = Status.StartsWith("Capturado", StringComparison.Ordinal)
			? Status
			: _routeMode == ActivityRouteMode.Free
				? "Siguiente más cercano"
				: "Siguiente objetivo";
		Recalculate();
	}

	private async Task TryAdvanceToNextPoiAsync()
	{
		if (_activityId is null || _userId is null)
			return;

		try
		{
			var detail = await _api.GetActivityAsync(_activityId.Value, _userId);
			if (detail is not null)
			{
				_routeMode = detail.RouteMode;
				_routePois = MergeQueuedCaptures(
					detail.Pois.OrderBy(p => p.Order).ToList(),
					detail.Id,
					_userId.Value);
			}

			await AdvanceFromLocalRouteAsync();
		}
		catch
		{
			await AdvanceFromLocalRouteAsync();
		}
	}

	private List<ActivityPoiDto> MergeQueuedCaptures(
		List<ActivityPoiDto> pois,
		Guid activityId,
		Guid userId)
	{
		var merged = pois;
		foreach (var q in _captureQueue.Snapshot()
			.Where(x => x.ActivityId == activityId && x.UserId == userId))
		{
			merged = MarkPoiCaptured(merged, q.PoiId, q.CapturedAt);
		}

		return merged;
	}

	private static List<ActivityPoiDto> MarkPoiCaptured(
		IEnumerable<ActivityPoiDto> pois,
		Guid poiId,
		DateTimeOffset capturedAt)
		=> pois.Select(p => p.PoiId == poiId
			? p with { Captured = true, CapturedAt = capturedAt }
			: p).ToList();

	private async Task CompleteRouteAsync(string activityTitle)
	{
		Status = "¡Ruta completada!";
		TargetLabel = "Fin";
		Clue = "";
		_targetLat = null;
		_targetLon = null;
		_useApiCapture = false;
		_captureFlash = 1;
		_captureFx.PlayCaptureSuccess();
		PushFrame();

		if (IsRunning)
			await StopAsync();

		if (_activityId is Guid aid && _userId is Guid uid)
		{
			RouteFinished?.Invoke(this, new RouteFinishedEventArgs(aid, uid, activityTitle));
		}
	}

	private void PushFrame()
	{
		Frame.RelativeBearingDegrees = RelativeBearingDegrees;
		Frame.DistanceMeters = DistanceMeters ?? double.NaN;
		Frame.PitchDegrees = _pitchDegrees;
		Frame.IsInProximity = IsInProximity;
		Frame.IsLocked = IsLocked;
		Frame.IsRunning = IsRunning;
		Frame.HasTarget = _targetLat is not null;
		Frame.Pulse = Pulse;
		Frame.CaptureFlash = _captureFlash;
		Frame.Status = Status;
		Frame.TargetLabel = TargetLabel;
		Frame.Clue = Clue;
		FrameUpdated?.Invoke(this, EventArgs.Empty);
	}

	private static double SmoothRelativeBearing(double current, double target, double alpha)
	{
		var delta = ((target - current + 540d) % 360d) - 180d;
		return current + delta * alpha;
	}
}

public sealed class RouteFinishedEventArgs(Guid activityId, Guid userId, string activityTitle) : EventArgs
{
	public Guid ActivityId { get; } = activityId;
	public Guid UserId { get; } = userId;
	public string ActivityTitle { get; } = activityTitle;
}