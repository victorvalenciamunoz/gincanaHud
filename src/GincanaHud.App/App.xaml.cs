namespace GincanaHud.App;

public partial class App : Application
{
	private readonly AppRoot _root;

	public App(AppRoot root, Services.ICaptureSyncService captureSync)
	{
		InitializeComponent();
		_root = root;
		captureSync.Start();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var nav = new NavigationPage(_root)
		{
			BarBackgroundColor = Color.FromArgb("#0B1218"),
			BarTextColor = Color.FromArgb("#E8EEF4")
		};
		return new Window(nav);
	}
}
