using GincanaHud.App.ViewModels;
using ZXing.Net.Maui;

namespace GincanaHud.App;

public partial class ScanPage : ContentPage
{
	private readonly JoinViewModel _join;
	private bool _done;
	private bool _started;

	public ScanPage(JoinViewModel join)
	{
		InitializeComponent();
		_join = join;

		Unloaded += (_, _) =>
		{
			try
			{
				Camera.IsDetecting = false;
				Camera.Handler?.DisconnectHandler();
			}
			catch
			{
				/* ignore teardown races */
			}
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		_done = false;
		_started = false;
		Hint.Text = "";

		// Dejar que el layout monte el handler antes de tocar la cámara.
		await Task.Delay(250);
		if (!IsLoaded)
			return;

		try
		{
			Camera.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormat.QrCode,
				AutoRotate = true,
				Multiple = false,
				TryHarder = true
			};
			Camera.IsDetecting = true;
			_started = true;
		}
		catch (Exception ex)
		{
			Hint.Text = $"No se pudo abrir la cámara: {ex.Message}";
		}
	}

	protected override void OnDisappearing()
	{
		try
		{
			Camera.IsDetecting = false;
		}
		catch
		{
			/* ignore */
		}

		base.OnDisappearing();
	}

	private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		if (_done || !_started)
			return;

		var value = e.Results?.FirstOrDefault()?.Value;
		if (string.IsNullOrWhiteSpace(value))
			return;

		_done = true;
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				Camera.IsDetecting = false;
			}
			catch
			{
				/* ignore */
			}

			_join.ApplyScannedCode(value);
			if (Navigation.ModalStack.Count > 0)
				await Navigation.PopModalAsync();
		});
	}

	private async void OnCancelClicked(object? sender, EventArgs e)
	{
		try
		{
			Camera.IsDetecting = false;
		}
		catch
		{
			/* ignore */
		}

		if (Navigation.ModalStack.Count > 0)
			await Navigation.PopModalAsync();
	}
}
