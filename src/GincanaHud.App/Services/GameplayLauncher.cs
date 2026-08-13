namespace GincanaHud.App.Services;

/// <summary>Señal de un solo uso: tras Unirse (o cold start con sesión) el HUD arranca solo.</summary>
public interface IGameplayLauncher
{
	void RequestAutoStart();
	bool ConsumeAutoStart();
}

public sealed class GameplayLauncher : IGameplayLauncher
{
	private int _pending;

	public void RequestAutoStart() => Interlocked.Exchange(ref _pending, 1);

	public bool ConsumeAutoStart() => Interlocked.Exchange(ref _pending, 0) == 1;
}
