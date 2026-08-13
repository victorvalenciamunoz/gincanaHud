namespace GincanaHud.App.Services;

public sealed record JoinSession(
	Guid UserId,
	Guid ActivityId,
	string ActivityTitle,
	string DisplayName,
	string JoinCode);

public sealed record JoinProfile(
	string DisplayName,
	string JoinCode,
	string ContactEmail,
	string ContactPhone);

public interface IJoinSessionStore
{
	JoinSession? Current { get; }
	JoinProfile LastProfile { get; }
	void Save(JoinSession session, JoinProfile profile);
	void SaveProfile(JoinProfile profile);
	/// <summary>Sale de la actividad pero conserva nombre/código/contacto.</summary>
	void ClearSession();
}

public sealed class PreferencesJoinSessionStore : IJoinSessionStore
{
	const string UserIdKey = "join_user_id";
	const string ActivityIdKey = "join_activity_id";
	const string ActivityTitleKey = "join_activity_title";
	const string DisplayNameKey = "join_display_name";
	const string JoinCodeKey = "join_code";
	const string EmailKey = "join_email";
	const string PhoneKey = "join_phone";

	public JoinSession? Current
	{
		get
		{
			var userRaw = Preferences.Default.Get(UserIdKey, "");
			var activityRaw = Preferences.Default.Get(ActivityIdKey, "");
			if (!Guid.TryParse(userRaw, out var userId) || !Guid.TryParse(activityRaw, out var activityId))
				return null;

			return new JoinSession(
				userId,
				activityId,
				Preferences.Default.Get(ActivityTitleKey, ""),
				Preferences.Default.Get(DisplayNameKey, ""),
				Preferences.Default.Get(JoinCodeKey, ""));
		}
	}

	public JoinProfile LastProfile => new(
		Preferences.Default.Get(DisplayNameKey, ""),
		Preferences.Default.Get(JoinCodeKey, ""),
		Preferences.Default.Get(EmailKey, ""),
		Preferences.Default.Get(PhoneKey, ""));

	public void Save(JoinSession session, JoinProfile profile)
	{
		Preferences.Default.Set(UserIdKey, session.UserId.ToString());
		Preferences.Default.Set(ActivityIdKey, session.ActivityId.ToString());
		Preferences.Default.Set(ActivityTitleKey, session.ActivityTitle);
		SaveProfile(profile with
		{
			DisplayName = session.DisplayName,
			JoinCode = session.JoinCode
		});
	}

	public void SaveProfile(JoinProfile profile)
	{
		Preferences.Default.Set(DisplayNameKey, profile.DisplayName);
		Preferences.Default.Set(JoinCodeKey, profile.JoinCode);
		Preferences.Default.Set(EmailKey, profile.ContactEmail);
		Preferences.Default.Set(PhoneKey, profile.ContactPhone);
	}

	public void ClearSession()
	{
		Preferences.Default.Remove(UserIdKey);
		Preferences.Default.Remove(ActivityIdKey);
		Preferences.Default.Remove(ActivityTitleKey);
	}
}
