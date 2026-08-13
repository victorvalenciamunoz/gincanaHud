using System.ComponentModel;
using GincanaHud.App.Services;
using GincanaHud.App.ViewModels;
using Microsoft.Maui.Devices;

namespace GincanaHud.App;

public partial class HudPage : ContentPage
{
	private readonly HudViewModel _viewModel;
	private readonly IServiceProvider _services;
	private readonly IGameplayLauncher _launcher;
	private bool _finishOpen;
	private bool _autoStartInFlight;
	private bool _introInFlight;

	public HudPage(HudViewModel viewModel, IServiceProvider services, IGameplayLauncher launcher)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_services = services;
		_launcher = launcher;
		BindingContext = viewModel;
		viewModel.FrameUpdated += OnFrameUpdated;
		viewModel.ProgressRingInvalidateRequested += OnProgressRingInvalidate;
		viewModel.CameraStartRequested += OnCameraStartRequested;
		viewModel.CameraStopRequested += OnCameraStopRequested;
		viewModel.RouteFinished += OnRouteFinished;
	}

	private void OnProgressRingInvalidate(object? sender, EventArgs e)
	{
		ProgressRingView.Invalidate();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		_viewModel.StartLinkMonitor();
		UpdateKeepScreenOn();

		if (_launcher.ConsumeAutoStart())
			_ = PlayIntroAndStartAsync();
	}

	private async Task PlayIntroAndStartAsync()
	{
		if (_autoStartInFlight || _viewModel.IsRunning)
			return;

		_autoStartInFlight = true;
		try
		{
			await ShowIntroAsync("Preparando cámara y sensores…");
			await _viewModel.EnsureStartedAsync();
			IntroSubtitle.Text = _viewModel.IsRunning
				? "La pista está ahí fuera."
				: _viewModel.Status;
			await Task.Delay(280);
			await HideIntroAsync();
		}
		finally
		{
			_autoStartInFlight = false;
		}
	}

	private async Task ShowIntroAsync(string subtitle)
	{
		if (_introInFlight)
			return;

		_introInFlight = true;
		IntroSubtitle.Text = subtitle;
		IntroOverlay.Opacity = 1;
		IntroOverlay.IsVisible = true;
		try
		{
			HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		}
		catch
		{
			/* sin háptica */
		}

		// Deja ver la marca un instante antes de que arranque el start.
		await Task.Delay(420);
	}

	private async Task HideIntroAsync()
	{
		try
		{
			await IntroOverlay.FadeToAsync(0, 480, Easing.CubicIn);
		}
		catch
		{
			IntroOverlay.Opacity = 0;
		}

		IntroOverlay.IsVisible = false;
		IntroOverlay.Opacity = 1;
		_introInFlight = false;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
		DeviceDisplay.Current.KeepScreenOn = false;
		_viewModel.StopLinkMonitor();
		try
		{
			Camera.StopCameraPreview();
		}
		catch
		{
			// Ignore teardown races.
		}
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(HudViewModel.IsRunning) or null)
			UpdateKeepScreenOn();
	}

	private void UpdateKeepScreenOn()
	{
		// Mientras se sigue una pista, no dejar que el idle apague la pantalla.
		DeviceDisplay.Current.KeepScreenOn = _viewModel.IsRunning;
	}

	private async void OnCameraStartRequested(object? sender, EventArgs e)
	{
		try
		{
			await Camera.StartCameraPreview(CancellationToken.None);
		}
		catch (Exception ex)
		{
			_viewModel.Status = $"Cámara: {ex.Message}";
		}
	}

	private void OnCameraStopRequested(object? sender, EventArgs e)
	{
		try
		{
			Camera.StopCameraPreview();
		}
		catch
		{
			// Ignore.
		}
	}

	private void OnFrameUpdated(object? sender, EventArgs e)
	{
		HudCanvas.Invalidate();
	}

	private async void OnRouteFinished(object? sender, RouteFinishedEventArgs e)
	{
		if (_finishOpen)
			return;

		_finishOpen = true;
		try
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				var page = _services.GetRequiredService<FinishPage>();
				await page.LoadAsync(e.ActivityId, e.UserId, e.ActivityTitle);
				await Navigation.PushModalAsync(page);
			});
		}
		catch (Exception ex)
		{
			_viewModel.Status = $"Fin: {ex.Message}";
		}
		finally
		{
			_finishOpen = false;
		}
	}
}
