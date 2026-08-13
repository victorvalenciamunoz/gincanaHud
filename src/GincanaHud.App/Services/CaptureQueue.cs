using System.Text.Json;

namespace GincanaHud.App.Services;

public sealed record PendingCapture(
	Guid QueueId,
	Guid ActivityId,
	Guid UserId,
	Guid PoiId,
	double Latitude,
	double Longitude,
	DateTimeOffset CapturedAt,
	int Attempts = 0);

public interface ICaptureQueue
{
	int Count { get; }
	event EventHandler? Changed;
	IReadOnlyList<PendingCapture> Snapshot();
	void Enqueue(PendingCapture item);
	void Remove(Guid queueId);
	void ReplaceAll(IEnumerable<PendingCapture> items);
}

/// <summary>Cola persistente en Preferences para capturas cuando no hay API.</summary>
public sealed class PreferencesCaptureQueue : ICaptureQueue
{
	private const string Key = "capture_queue_v1";
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
	private readonly object _gate = new();
	private List<PendingCapture> _items;

	public PreferencesCaptureQueue()
	{
		_items = Load();
	}

	public int Count
	{
		get
		{
			lock (_gate)
				return _items.Count;
		}
	}

	public event EventHandler? Changed;

	public IReadOnlyList<PendingCapture> Snapshot()
	{
		lock (_gate)
			return _items.ToList();
	}

	public void Enqueue(PendingCapture item)
	{
		lock (_gate)
		{
			// Idempotencia local: un POI por actividad/usuario.
			_items.RemoveAll(x =>
				x.ActivityId == item.ActivityId &&
				x.UserId == item.UserId &&
				x.PoiId == item.PoiId);
			_items.Add(item);
			PersistUnlocked();
		}

		Changed?.Invoke(this, EventArgs.Empty);
	}

	public void Remove(Guid queueId)
	{
		lock (_gate)
		{
			var n = _items.RemoveAll(x => x.QueueId == queueId);
			if (n == 0)
				return;
			PersistUnlocked();
		}

		Changed?.Invoke(this, EventArgs.Empty);
	}

	public void ReplaceAll(IEnumerable<PendingCapture> items)
	{
		lock (_gate)
		{
			_items = items.ToList();
			PersistUnlocked();
		}

		Changed?.Invoke(this, EventArgs.Empty);
	}

	private void PersistUnlocked()
	{
		var json = JsonSerializer.Serialize(_items, JsonOptions);
		Preferences.Default.Set(Key, json);
	}

	private static List<PendingCapture> Load()
	{
		var raw = Preferences.Default.Get(Key, "");
		if (string.IsNullOrWhiteSpace(raw))
			return [];

		try
		{
			return JsonSerializer.Deserialize<List<PendingCapture>>(raw, JsonOptions) ?? [];
		}
		catch
		{
			return [];
		}
	}
}
