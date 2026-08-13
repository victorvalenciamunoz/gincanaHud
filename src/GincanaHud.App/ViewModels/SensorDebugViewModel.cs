using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GincanaHud.App.Services;

namespace GincanaHud.App.ViewModels;

public partial class SensorDebugViewModel : ObservableObject
{
	private readonly ILocationService _location;
	private readonly ICompassService _compass;

	private double? _latitude;
	private double? _longitude;
	private double? _accuracyMeters;
	private double? _headingDegrees;
	private string _status = "Listo";
	private bool _isRunning;

	public SensorDebugViewModel(ILocationService location, ICompassService compass)
	{
		_location = location;
		_compass = compass;
		ToggleCommand = new Command(async () => await ToggleAsync());

		_location.PositionChanged += (_, pos) =>
			MainThread.BeginInvokeOnMainThread(() => ApplyPosition(pos));

		_compass.HeadingChanged += (_, reading) =>
			MainThread.BeginInvokeOnMainThread(() =>
			{
				HeadingDegrees = reading.HeadingDegrees;
				Status = "Sensores activos";
			});
	}

	// Explicit properties so XAML C# Expressions SourceGen can resolve them
	// (CommunityToolkit [ObservableProperty] members are not visible to XamlSourceGen yet).
	public double? Latitude
	{
		get => _latitude;
		set => SetProperty(ref _latitude, value);
	}

	public double? Longitude
	{
		get => _longitude;
		set => SetProperty(ref _longitude, value);
	}

	public double? AccuracyMeters
	{
		get => _accuracyMeters;
		set => SetProperty(ref _accuracyMeters, value);
	}

	public double? HeadingDegrees
	{
		get => _headingDegrees;
		set => SetProperty(ref _headingDegrees, value);
	}

	public string Status
	{
		get => _status;
		set => SetProperty(ref _status, value);
	}

	public bool IsRunning
	{
		get => _isRunning;
		set => SetProperty(ref _isRunning, value);
	}

	public ICommand ToggleCommand { get; }

	private async Task ToggleAsync()
	{
		if (IsRunning)
		{
			await StopAsync();
			return;
		}

		await StartAsync();
	}

	private async Task StartAsync()
	{
		try
		{
			Status = "Solicitando permisos / iniciando…";
			await _location.StartAsync();
			_compass.Start();
			IsRunning = true;
			Status = "Sensores activos";
		}
		catch (Exception ex)
		{
			IsRunning = false;
			Status = ex.Message;
		}
	}

	private async Task StopAsync()
	{
		await _location.StopAsync();
		_compass.Stop();
		IsRunning = false;
		Status = "Detenido";
	}

	private void ApplyPosition(GeoPosition pos)
	{
		Latitude = pos.Latitude;
		Longitude = pos.Longitude;
		AccuracyMeters = pos.AccuracyMeters;
	}
}
