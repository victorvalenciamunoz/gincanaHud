using GincanaHud.App.ViewModels;

namespace GincanaHud.App;

public partial class JoinPage : ContentPage
{
	private readonly JoinViewModel _vm;
	private readonly IServiceProvider _services;

	public JoinPage(JoinViewModel vm, IServiceProvider services)
	{
		InitializeComponent();
		_vm = vm;
		_services = services;
		BindingContext = vm;
#if DEBUG
		SensorsDebugButton.IsVisible = true;
#endif
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_vm.RefreshSession();
	}

	private async void OnScanClicked(object? sender, EventArgs e)
	{
		try
		{
			var camera = await Permissions.RequestAsync<Permissions.Camera>();
			if (camera != PermissionStatus.Granted)
			{
				_vm.NotifyStatus("Permiso de cámara denegado.");
				return;
			}

			var scan = _services.GetRequiredService<ScanPage>();
			await Navigation.PushModalAsync(scan);
		}
		catch (Exception ex)
		{
			_vm.NotifyStatus($"Escáner: {ex.Message}");
		}
	}

	private async void OnSensorsDebugClicked(object? sender, EventArgs e)
	{
#if DEBUG
		try
		{
			var page = _services.GetRequiredService<MainPage>();
			page.ToolbarItems.Clear();
			page.ToolbarItems.Add(new ToolbarItem(
				"Cerrar",
				null,
				async () =>
				{
					if (Navigation.ModalStack.Count > 0)
						await Navigation.PopModalAsync();
				}));
			await Navigation.PushModalAsync(new NavigationPage(page)
			{
				BarBackgroundColor = Color.FromArgb("#0B1218"),
				BarTextColor = Color.FromArgb("#E8EEF4")
			});
		}
		catch (Exception ex)
		{
			_vm.NotifyStatus($"Sensores: {ex.Message}");
		}
#endif
	}
}
