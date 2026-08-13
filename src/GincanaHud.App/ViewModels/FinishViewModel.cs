using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GincanaHud.App.Services;
using GincanaHud.Shared;

namespace GincanaHud.App.ViewModels;

public sealed class FinishViewModel : ObservableObject
{
	private readonly IGincanaApiClient _api;
	private readonly IAppNavigator _nav;
	private readonly IJoinSessionStore _session;
	private readonly ICaptureSyncService _sync;
	private readonly ICaptureQueue _queue;

	private Guid _activityId;
	private Guid _userId;
	private string _activityTitle = "";
	private string _headline = "¡Ruta completada!";
	private string _subtitle = "";
	private string _myStats = "";
	private string _rankLine = "";
	private bool _busy = true;
	private bool _hasPodium;
	private string? _error;

	public FinishViewModel(
		IGincanaApiClient api,
		IAppNavigator nav,
		IJoinSessionStore session,
		ICaptureSyncService sync,
		ICaptureQueue queue)
	{
		_api = api;
		_nav = nav;
		_session = session;
		_sync = sync;
		_queue = queue;
		CloseCommand = new Command(async () => await CloseAsync());
		LeaveCommand = new Command(async () => await LeaveAsync());
	}

	public ObservableCollection<RankingRow> Ranking { get; } = [];
	public ObservableCollection<PodiumSlot> Podium { get; } = [];

	public string ActivityTitle
	{
		get => _activityTitle;
		private set => SetProperty(ref _activityTitle, value);
	}

	public string Headline
	{
		get => _headline;
		private set => SetProperty(ref _headline, value);
	}

	public string Subtitle
	{
		get => _subtitle;
		private set => SetProperty(ref _subtitle, value);
	}

	public string MyStats
	{
		get => _myStats;
		private set => SetProperty(ref _myStats, value);
	}

	public string RankLine
	{
		get => _rankLine;
		private set => SetProperty(ref _rankLine, value);
	}

	public bool Busy
	{
		get => _busy;
		private set => SetProperty(ref _busy, value);
	}

	public bool HasPodium
	{
		get => _hasPodium;
		private set => SetProperty(ref _hasPodium, value);
	}

	public string? Error
	{
		get => _error;
		private set => SetProperty(ref _error, value);
	}

	public ICommand CloseCommand { get; }
	public ICommand LeaveCommand { get; }

	public Func<Task>? CloseModalAsync { get; set; }

	public async Task LoadAsync(Guid activityId, Guid userId, string activityTitle)
	{
		_activityId = activityId;
		_userId = userId;
		ActivityTitle = activityTitle;
		_sync.Flushed -= OnFlushed;
		_sync.Flushed += OnFlushed;
		await _sync.FlushAsync();
		await ReloadRankingAsync();
	}

	private void OnFlushed(object? sender, EventArgs e)
		=> MainThread.BeginInvokeOnMainThread(async () => await ReloadRankingAsync());

	private async Task ReloadRankingAsync()
	{
		Busy = true;
		Error = null;
		Ranking.Clear();
		Podium.Clear();
		HasPodium = false;

		try
		{
			var pending = _queue.Snapshot()
				.Count(x => x.ActivityId == _activityId && x.UserId == _userId);

			var ranking = await _api.GetRankingAsync(_activityId);
			var finishers = ranking.Where(r => r.FinishedAt is not null).ToList();
			var me = ranking.FirstOrDefault(r => r.UserId == _userId);
			var placeAmongFinishers = me?.FinishedAt is null
				? -1
				: finishers.FindIndex(r => r.UserId == _userId) + 1;

			if (pending > 0 && me?.FinishedAt is null)
			{
				MyStats = "Meta guardada en el móvil";
				RankLine = "Sincronizando con la API…";
				Subtitle = "Sin red en la meta — el puesto aparecerá al recuperar la API.";
			}
			else if (me?.FinishedAt is { } finishedAt)
			{
				MyStats = $"Meta a las {finishedAt.ToLocalTime():HH:mm:ss}";
				RankLine = placeAmongFinishers > 0
					? $"Llegada #{placeAmongFinishers} de {finishers.Count}"
					: "";
				Subtitle = placeAmongFinishers == 1
					? "Has sido el primero en la meta. Brutal."
					: "Buen trabajo — mira el orden de llegada.";
			}
			else if (me is not null)
			{
				MyStats = $"{me.CaptureCount} puntos de la ruta";
				RankLine = "";
				Subtitle = "Aún no has llegado a la meta.";
			}
			else
			{
				MyStats = "Sin capturas registradas.";
				RankLine = "";
				Subtitle = "La ruta está completa.";
			}

			var placeColors = new[] { "#FFC42E", "#C5D0DB", "#D4956A" };
			var podiumFinishers = finishers.Take(3).ToList();
			if (podiumFinishers.Count > 0)
			{
				// Visual: 2º | 1º | 3º
				PodiumSlot? second = null, first = null, third = null;
				for (var i = 0; i < podiumFinishers.Count; i++)
				{
					var row = podiumFinishers[i];
					var slot = new PodiumSlot(
						i + 1,
						row.DisplayName,
						row.FinishedAt!.Value.ToLocalTime().ToString("HH:mm:ss"),
						placeColors[i],
						row.UserId == _userId,
						BarHeight: i == 0 ? 88 : i == 1 ? 68 : 56);
					if (i == 0) first = slot;
					else if (i == 1) second = slot;
					else third = slot;
				}

				if (second is not null) Podium.Add(second);
				if (first is not null) Podium.Add(first);
				if (third is not null) Podium.Add(third);
				HasPodium = true;
			}

			var iRow = 1;
			foreach (var row in ranking.Take(10))
			{
				var detail = row.FinishedAt is { } at
					? at.ToLocalTime().ToString("HH:mm:ss")
					: $"{row.CaptureCount} pts ruta";
				Ranking.Add(new RankingRow(
					iRow,
					row.DisplayName,
					detail,
					row.FinishedAt is not null,
					row.UserId == _userId));
				iRow++;
			}
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			Subtitle = _queue.Count > 0
				? "Completaste la ruta. El ranking se actualizará al sincronizar."
				: "Completaste la ruta (no se pudo cargar el ranking).";
		}
		finally
		{
			Busy = false;
		}
	}

	private async Task CloseAsync()
	{
		_sync.Flushed -= OnFlushed;
		if (CloseModalAsync is not null)
			await CloseModalAsync();
	}

	private async Task LeaveAsync()
	{
		_sync.Flushed -= OnFlushed;
		_session.ClearSession();
		_nav.GoToJoin();
		if (CloseModalAsync is not null)
			await CloseModalAsync();
	}
}

public sealed record RankingRow(int Place, string Name, string Detail, bool Finished, bool IsMe);

public sealed record PodiumSlot(
	int Place,
	string Name,
	string Time,
	string Accent,
	bool IsMe,
	double BarHeight);
