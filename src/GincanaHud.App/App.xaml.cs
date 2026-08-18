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
		// TabbedPage como raíz. Un NavigationPage extra crea fragmentos Android
		// (id jumpToEnd / navigationlayout_content) que fallan tras deploys incrementales.
		return new Window(_root);
	}
}
