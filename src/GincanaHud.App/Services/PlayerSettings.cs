namespace GincanaHud.App.Services;

public interface IPlayerSettings
{
	bool SoundEnabled { get; set; }
	event EventHandler? Changed;
}

public sealed class PreferencesPlayerSettings : IPlayerSettings
{
	const string SoundKey = "settings_sound_enabled";

	public bool SoundEnabled
	{
		get => Preferences.Default.Get(SoundKey, true);
		set
		{
			if (Preferences.Default.Get(SoundKey, true) == value)
				return;
			Preferences.Default.Set(SoundKey, value);
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? Changed;
}
