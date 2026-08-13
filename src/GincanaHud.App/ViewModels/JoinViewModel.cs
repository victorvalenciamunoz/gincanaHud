using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GincanaHud.App.Services;
using GincanaHud.Shared;

namespace GincanaHud.App.ViewModels;

public sealed class JoinViewModel : ObservableObject
{
	private readonly IGincanaApiClient _api;
	private readonly IJoinSessionStore _session;
	private readonly IAppNavigator _nav;
	private readonly IPlayerSettings _settings;
	private readonly IGameplayLauncher _launcher;

	private string _joinCode = "";
	private string _displayName = "";
	private string _contactEmail = "";
	private string _contactPhone = "";
	private string _status = "Introduce el código o escanea el QR del cartel.";
	private string _sessionSummary = "";
	private bool _busy;
	private bool _hasSession;
	private bool _soundEnabled;

	public JoinViewModel(
		IGincanaApiClient api,
		IJoinSessionStore session,
		IAppNavigator nav,
		IPlayerSettings settings,
		IGameplayLauncher launcher)
	{
		_api = api;
		_session = session;
		_nav = nav;
		_settings = settings;
		_launcher = launcher;
		_soundEnabled = settings.SoundEnabled;
		JoinCommand = new Command(async () => await JoinAsync(), () => !_busy);
		ClearSessionCommand = new Command(ClearSession, () => HasSession && !_busy);
		LoadFormFromProfile();
		RefreshSession();
	}

	public string JoinCode
	{
		get => _joinCode;
		set => SetProperty(ref _joinCode, value);
	}

	public string DisplayName
	{
		get => _displayName;
		set => SetProperty(ref _displayName, value);
	}

	public string ContactEmail
	{
		get => _contactEmail;
		set => SetProperty(ref _contactEmail, value);
	}

	public string ContactPhone
	{
		get => _contactPhone;
		set => SetProperty(ref _contactPhone, value);
	}

	public string Status
	{
		get => _status;
		private set => SetProperty(ref _status, value);
	}

	public string SessionSummary
	{
		get => _sessionSummary;
		private set => SetProperty(ref _sessionSummary, value);
	}

	public bool Busy
	{
		get => _busy;
		private set
		{
			if (SetProperty(ref _busy, value))
			{
				((Command)JoinCommand).ChangeCanExecute();
				((Command)ClearSessionCommand).ChangeCanExecute();
			}
		}
	}

	public bool HasSession
	{
		get => _hasSession;
		private set
		{
			if (SetProperty(ref _hasSession, value))
				((Command)ClearSessionCommand).ChangeCanExecute();
		}
	}

	public bool SoundEnabled
	{
		get => _soundEnabled;
		set
		{
			if (!SetProperty(ref _soundEnabled, value))
				return;
			_settings.SoundEnabled = value;
		}
	}

	public ICommand JoinCommand { get; }
	public ICommand ClearSessionCommand { get; }

	public void RefreshSession()
	{
		LoadFormFromProfile();
		var current = _session.Current;
		HasSession = current is not null;
		if (current is null)
		{
			SessionSummary = "Sin actividad unida.";
			return;
		}

		SessionSummary = $"{current.DisplayName} · {current.ActivityTitle} ({current.JoinCode})";
		DisplayName = current.DisplayName;
		JoinCode = current.JoinCode;
	}

	public void ApplyScannedCode(string code)
	{
		JoinCode = code.Trim().ToUpperInvariant();
		PersistProfileDraft();
		Status = $"Código leído: {JoinCode}";
	}

	public void NotifyStatus(string message) => Status = message;

	private void LoadFormFromProfile()
	{
		var p = _session.LastProfile;
		if (string.IsNullOrWhiteSpace(DisplayName) && !string.IsNullOrWhiteSpace(p.DisplayName))
			DisplayName = p.DisplayName;
		if (string.IsNullOrWhiteSpace(JoinCode) && !string.IsNullOrWhiteSpace(p.JoinCode))
			JoinCode = p.JoinCode;
		if (string.IsNullOrWhiteSpace(ContactEmail) && !string.IsNullOrWhiteSpace(p.ContactEmail))
			ContactEmail = p.ContactEmail;
		if (string.IsNullOrWhiteSpace(ContactPhone) && !string.IsNullOrWhiteSpace(p.ContactPhone))
			ContactPhone = p.ContactPhone;
	}

	private void PersistProfileDraft()
	{
		_session.SaveProfile(new JoinProfile(
			DisplayName.Trim(),
			JoinCode.Trim().ToUpperInvariant(),
			ContactEmail.Trim(),
			ContactPhone.Trim()));
	}

	private async Task JoinAsync()
	{
		var code = JoinCode.Trim().ToUpperInvariant();
		var name = DisplayName.Trim();
		if (code.Length < 4)
		{
			Status = "Código demasiado corto.";
			return;
		}

		if (name.Length < 2)
		{
			Status = "Pon tu nombre (mín. 2 caracteres).";
			return;
		}

		Busy = true;
		Status = "Uniéndote…";
		PersistProfileDraft();
		try
		{
			var result = await _api.JoinAsync(new JoinActivityRequest(
				code,
				name,
				string.IsNullOrWhiteSpace(ContactEmail) ? null : ContactEmail.Trim(),
				string.IsNullOrWhiteSpace(ContactPhone) ? null : ContactPhone.Trim()));

			_session.Save(
				new JoinSession(
					result.User.Id,
					result.Activity.Id,
					result.Activity.Title,
					result.User.DisplayName,
					result.Activity.JoinCode),
				new JoinProfile(
					result.User.DisplayName,
					result.Activity.JoinCode,
					ContactEmail.Trim(),
					ContactPhone.Trim()));

			RefreshSession();
			var modeNote = result.Activity.RouteMode == ActivityRouteMode.Free
				? "Ruta libre"
				: "Ruta secuencial";
			Status = $"Listo: {result.Activity.Title} ({modeNote}). Arrancando…";
			_launcher.RequestAutoStart();
			_nav.GoToHud();
		}
		catch (Exception ex)
		{
			Status = ex.Message;
		}
		finally
		{
			Busy = false;
		}
	}

	private void ClearSession()
	{
		PersistProfileDraft();
		_session.ClearSession();
		RefreshSession();
		Status = "Saliste de la actividad. Tus datos se conservan para volver a unirte.";
		_nav.GoToJoin();
	}
}
